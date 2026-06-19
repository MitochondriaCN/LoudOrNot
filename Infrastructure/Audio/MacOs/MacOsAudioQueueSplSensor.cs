using System.Runtime.InteropServices;
using LoudOrNot.Domain.Audio;
using LoudOrNot.Infrastructure.Audio.Common;

namespace LoudOrNot.Infrastructure.Audio.MacOs;

public sealed class MacOsAudioQueueSplSensor(string deviceUid, string deviceName) : ISplSensor
{
    private const uint AudioFormatLinearPcm = 0x6C70636D;
    private const uint AudioFormatFlagIsSignedInteger = 4;
    private const uint AudioFormatFlagIsPacked = 8;
    private const uint AudioQueuePropertyCurrentDevice = 0x61716364;
    private const uint CFStringEncodingUtf8 = 0x08000100;
    private const int NoError = 0;

    public string Name { get; } = $"{deviceName} AudioQueue SPL sensor";
    public string Description { get; } = "通过 macOS AudioQueue 采集指定输入设备 PCM 数据并估算瞬时声压级。";

    public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl()
    {
        return Task.Run(CaptureOnce)
            .WaitAsync(TimeSpan.FromSeconds(3))
            .GetAwaiter()
            .GetResult();
    }

    private InstantaneousAmbientSpl CaptureOnce()
    {
        var state = new CaptureState();
        var stateHandle = GCHandle.Alloc(state);
        var queue = IntPtr.Zero;
        var currentDeviceUid = IntPtr.Zero;
        var currentDeviceUidPropertyData = IntPtr.Zero;
        AudioQueueInputCallbackDelegate callback = AudioQueueInputCallback;

        try
        {
            var format = AudioStreamBasicDescription.CreatePcm16Mono();
            var result = AudioQueueNewInput(
                ref format,
                callback,
                GCHandle.ToIntPtr(stateHandle),
                IntPtr.Zero,
                IntPtr.Zero,
                0,
                out queue);
            ThrowIfAudioQueueError(result, "创建 macOS 输入音频队列失败");

            currentDeviceUid = CFStringCreateWithCString(IntPtr.Zero, deviceUid, CFStringEncodingUtf8);
            if (currentDeviceUid == IntPtr.Zero)
            {
                throw new InvalidOperationException($"创建 macOS 输入设备 UID 失败: {deviceUid}");
            }

            currentDeviceUidPropertyData = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(currentDeviceUidPropertyData, currentDeviceUid);
            result = AudioQueueSetProperty(
                queue,
                AudioQueuePropertyCurrentDevice,
                currentDeviceUidPropertyData,
                (uint)IntPtr.Size);
            ThrowIfAudioQueueError(result, $"设置 macOS 输入设备失败: {deviceName} ({deviceUid})");

            result = AudioQueueAllocateBuffer(queue, SplSamplingConstants.SampleByteCount, out var audioBuffer);
            ThrowIfAudioQueueError(result, "分配 macOS 输入音频缓冲区失败");

            result = AudioQueueEnqueueBuffer(queue, audioBuffer, 0, IntPtr.Zero);
            ThrowIfAudioQueueError(result, "提交 macOS 输入音频缓冲区失败");

            result = AudioQueueStart(queue, IntPtr.Zero);
            ThrowIfAudioQueueError(result, "启动 macOS 输入音频队列失败");

            if (!state.Completed.Wait(TimeSpan.FromSeconds(2)))
            {
                throw new TimeoutException("等待 macOS 输入采样超时。");
            }

            if (state.Exception is not null)
            {
                throw state.Exception;
            }

            return PcmSplEstimator.EstimateInt16LittleEndian(state.Buffer, this);
        }
        finally
        {
            GC.KeepAlive(callback);

            if (queue != IntPtr.Zero)
            {
                AudioQueueStop(queue, 1);
                AudioQueueDispose(queue, 1);
            }

            if (currentDeviceUidPropertyData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(currentDeviceUidPropertyData);
            }

            if (currentDeviceUid != IntPtr.Zero)
            {
                CFRelease(currentDeviceUid);
            }

            stateHandle.Free();
            state.Completed.Dispose();
        }
    }

    private static void AudioQueueInputCallback(
        IntPtr inUserData,
        IntPtr inAQ,
        IntPtr inBuffer,
        IntPtr inStartTime,
        uint inNumberPacketDescriptions,
        IntPtr inPacketDescs)
    {
        var handle = GCHandle.FromIntPtr(inUserData);
        var state = (CaptureState)handle.Target!;

        try
        {
            var audioBuffer = Marshal.PtrToStructure<AudioQueueBuffer>(inBuffer);
            if (audioBuffer.AudioDataByteSize == 0)
            {
                throw new InvalidOperationException("macOS 输入采样数据为空。");
            }

            var byteCount = Math.Min((int)audioBuffer.AudioDataByteSize, SplSamplingConstants.SampleByteCount);
            state.Buffer = new byte[byteCount];
            Marshal.Copy(audioBuffer.AudioData, state.Buffer, 0, byteCount);
        }
        catch (Exception exception)
        {
            state.Exception = exception;
        }
        finally
        {
            state.Completed.Set();
        }
    }

    private static void ThrowIfAudioQueueError(int status, string message)
    {
        if (status != NoError)
        {
            throw new InvalidOperationException($"{message}: CoreAudio status {status}");
        }
    }

    private sealed class CaptureState
    {
        public ManualResetEventSlim Completed { get; } = new();
        public byte[] Buffer { get; set; } = [];
        public Exception? Exception { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioStreamBasicDescription
    {
        public double SampleRate;
        public uint FormatId;
        public uint FormatFlags;
        public uint BytesPerPacket;
        public uint FramesPerPacket;
        public uint BytesPerFrame;
        public uint ChannelsPerFrame;
        public uint BitsPerChannel;
        public uint Reserved;

        public static AudioStreamBasicDescription CreatePcm16Mono()
        {
            return new AudioStreamBasicDescription
            {
                SampleRate = SplSamplingConstants.SampleRate,
                FormatId = AudioFormatLinearPcm,
                FormatFlags = AudioFormatFlagIsSignedInteger | AudioFormatFlagIsPacked,
                BytesPerPacket = SplSamplingConstants.BytesPerSample,
                FramesPerPacket = 1,
                BytesPerFrame = SplSamplingConstants.BytesPerSample,
                ChannelsPerFrame = SplSamplingConstants.Channels,
                BitsPerChannel = SplSamplingConstants.BitsPerSample
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioQueueBuffer
    {
        public uint AudioDataBytesCapacity;
        public IntPtr AudioData;
        public uint AudioDataByteSize;
        public IntPtr UserData;
        public uint PacketDescriptionCapacity;
        public IntPtr PacketDescriptions;
        public uint PacketDescriptionCount;
    }

    private delegate void AudioQueueInputCallbackDelegate(
        IntPtr inUserData,
        IntPtr inAQ,
        IntPtr inBuffer,
        IntPtr inStartTime,
        uint inNumberPacketDescriptions,
        IntPtr inPacketDescs);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueNewInput(
        ref AudioStreamBasicDescription inFormat,
        AudioQueueInputCallbackDelegate inCallbackProc,
        IntPtr inUserData,
        IntPtr inCallbackRunLoop,
        IntPtr inCallbackRunLoopMode,
        uint inFlags,
        out IntPtr outAQ);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueAllocateBuffer(
        IntPtr inAQ,
        int inBufferByteSize,
        out IntPtr outBuffer);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueSetProperty(
        IntPtr inAQ,
        uint inID,
        IntPtr inData,
        uint inDataSize);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueEnqueueBuffer(
        IntPtr inAQ,
        IntPtr inBuffer,
        uint inNumPacketDescs,
        IntPtr inPacketDescs);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueStart(
        IntPtr inAQ,
        IntPtr inStartTime);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueStop(
        IntPtr inAQ,
        byte inImmediate);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueDispose(
        IntPtr inAQ,
        byte inImmediate);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(
        IntPtr alloc,
        string cStr,
        uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);
}
