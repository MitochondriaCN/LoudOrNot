using System.Runtime.InteropServices;
using LoudOrNot.Domain.Audio;
using LoudOrNot.Infrastructure.Audio.Common;

namespace LoudOrNot.Infrastructure.Audio.Windows;

public sealed class WindowsWaveInSplSensor(string deviceId, string deviceName) : ISplSensor
{
    private const int WaveMapper = -1;
    private const int WaveFormatPcm = 1;
    private const int CallbackNull = 0;
    private const int MmsysErrNoError = 0;
    private const int WhdrDone = 1;

    public string Name { get; } = $"{deviceName} waveIn SPL sensor";
    public string Description { get; } = "通过 Windows waveIn 采集指定输入设备 PCM 数据并估算瞬时声压级。";

    public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl()
    {
        var format = WaveFormat.CreatePcm16Mono();
        var result = waveInOpen(out var waveIn, ToWaveInDeviceId(deviceId), ref format, IntPtr.Zero, IntPtr.Zero, CallbackNull);
        ThrowIfWaveError(result, "打开 Windows 输入设备失败");

        var buffer = new byte[SplSamplingConstants.SampleByteCount];
        var bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        var header = new WaveHeader
        {
            Data = bufferHandle.AddrOfPinnedObject(),
            BufferLength = buffer.Length
        };
        var headerHandle = GCHandle.Alloc(header, GCHandleType.Pinned);

        try
        {
            result = waveInPrepareHeader(waveIn, headerHandle.AddrOfPinnedObject(), Marshal.SizeOf<WaveHeader>());
            ThrowIfWaveError(result, "准备 Windows 输入采样缓冲区失败");

            result = waveInAddBuffer(waveIn, headerHandle.AddrOfPinnedObject(), Marshal.SizeOf<WaveHeader>());
            ThrowIfWaveError(result, "提交 Windows 输入采样缓冲区失败");

            result = waveInStart(waveIn);
            ThrowIfWaveError(result, "启动 Windows 输入采样失败");

            var deadline = DateTime.UtcNow.AddSeconds(2);
            do
            {
                Thread.Sleep(10);
                header = Marshal.PtrToStructure<WaveHeader>(headerHandle.AddrOfPinnedObject());
            }
            while ((header.Flags & WhdrDone) == 0 && DateTime.UtcNow < deadline);

            if ((header.Flags & WhdrDone) == 0)
            {
                throw new TimeoutException("等待 Windows 输入采样超时。");
            }

            return PcmSplEstimator.EstimateInt16LittleEndian(buffer.AsSpan(0, (int)header.BytesRecorded), this);
        }
        finally
        {
            waveInStop(waveIn);
            waveInUnprepareHeader(waveIn, headerHandle.AddrOfPinnedObject(), Marshal.SizeOf<WaveHeader>());
            waveInClose(waveIn);
            headerHandle.Free();
            bufferHandle.Free();
        }
    }

    private static void ThrowIfWaveError(int result, string message)
    {
        if (result == MmsysErrNoError)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: MMSYSERR {result}");
    }

    private static int ToWaveInDeviceId(string id)
    {
        return string.Equals(id, "default", StringComparison.OrdinalIgnoreCase)
            ? WaveMapper
            : int.Parse(id);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormat
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort Size;

        public static WaveFormat CreatePcm16Mono()
        {
            return new WaveFormat
            {
                FormatTag = WaveFormatPcm,
                Channels = SplSamplingConstants.Channels,
                SamplesPerSec = SplSamplingConstants.SampleRate,
                AvgBytesPerSec = SplSamplingConstants.SampleRate * SplSamplingConstants.Channels * SplSamplingConstants.BytesPerSample,
                BlockAlign = SplSamplingConstants.Channels * SplSamplingConstants.BytesPerSample,
                BitsPerSample = SplSamplingConstants.BitsPerSample
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr Data;
        public int BufferLength;
        public uint BytesRecorded;
        public IntPtr User;
        public uint Flags;
        public uint Loops;
        public IntPtr Next;
        public IntPtr Reserved;
    }

    [DllImport("winmm.dll")]
    private static extern int waveInOpen(
        out IntPtr waveIn,
        int deviceId,
        ref WaveFormat format,
        IntPtr callback,
        IntPtr instance,
        int flags);

    [DllImport("winmm.dll")]
    private static extern int waveInPrepareHeader(
        IntPtr waveIn,
        IntPtr header,
        int headerSize);

    [DllImport("winmm.dll")]
    private static extern int waveInAddBuffer(
        IntPtr waveIn,
        IntPtr header,
        int headerSize);

    [DllImport("winmm.dll")]
    private static extern int waveInStart(IntPtr waveIn);

    [DllImport("winmm.dll")]
    private static extern int waveInStop(IntPtr waveIn);

    [DllImport("winmm.dll")]
    private static extern int waveInUnprepareHeader(
        IntPtr waveIn,
        IntPtr header,
        int headerSize);

    [DllImport("winmm.dll")]
    private static extern int waveInClose(IntPtr waveIn);
}
