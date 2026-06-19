using System.Runtime.InteropServices;
using System.Text;
using LoudOrNot.Infrastructure.Audio.Common;

namespace LoudOrNot.Infrastructure.Audio.MacOs;

internal static class MacOsCoreAudio
{
    private const uint AudioObjectSystemObject = 1;
    private const uint AudioHardwarePropertyDevices = 0x64657623;
    private const uint AudioHardwarePropertyDefaultInputDevice = 0x64496E20;
    private const uint AudioDevicePropertyDeviceUid = 0x75696420;
    private const uint AudioDevicePropertyStreams = 0x73746D23;
    private const uint AudioObjectPropertyName = 0x6C6E616D;
    private const uint AudioObjectPropertyScopeGlobal = 0x676C6F62;
    private const uint AudioObjectPropertyScopeInput = 0x696E7074;
    private const uint AudioObjectPropertyElementMain = 0;
    private const uint CFStringEncodingUtf8 = 0x08000100;
    private const int NoError = 0;

    public static IReadOnlyList<MacOsAudioDeviceInfo> GetInputDevices()
    {
        var address = new AudioObjectPropertyAddress(
            AudioHardwarePropertyDevices,
            AudioObjectPropertyScopeGlobal,
            AudioObjectPropertyElementMain);
        var status = AudioObjectGetPropertyDataSize(
            AudioObjectSystemObject,
            ref address,
            0,
            IntPtr.Zero,
            out var dataSize);

        if (status != NoError || dataSize == 0)
        {
            return [];
        }

        var deviceIds = new uint[dataSize / Marshal.SizeOf<uint>()];
        status = AudioObjectGetPropertyData(
            AudioObjectSystemObject,
            ref address,
            0,
            IntPtr.Zero,
            ref dataSize,
            deviceIds);

        if (status != NoError)
        {
            return [];
        }

        return deviceIds
            .Where(HasInputStreams)
            .Select(deviceId => new MacOsAudioDeviceInfo(
                GetDeviceUid(deviceId),
                GetDeviceName(deviceId)))
            .Where(device => !string.IsNullOrWhiteSpace(device.Uid))
            .ToList();
    }

    public static uint GetDefaultInputDeviceId()
    {
        var address = new AudioObjectPropertyAddress(
            AudioHardwarePropertyDefaultInputDevice,
            AudioObjectPropertyScopeGlobal,
            AudioObjectPropertyElementMain);
        var dataSize = (uint)Marshal.SizeOf<uint>();
        var status = AudioObjectGetPropertyData(
            AudioObjectSystemObject,
            ref address,
            0,
            IntPtr.Zero,
            ref dataSize,
            out uint deviceId);

        if (status != NoError || deviceId == 0)
        {
            throw new InvalidOperationException(
                $"无法读取 macOS 当前默认输入设备: CoreAudio status {status}, device id {deviceId}。");
        }

        return deviceId;
    }

    public static string GetDeviceName(uint deviceId)
    {
        var address = new AudioObjectPropertyAddress(
            AudioObjectPropertyName,
            AudioObjectPropertyScopeGlobal,
            AudioObjectPropertyElementMain);
        var dataSize = (uint)IntPtr.Size;
        var status = AudioObjectGetPropertyData(
            deviceId,
            ref address,
            0,
            IntPtr.Zero,
            ref dataSize,
            out IntPtr cfString);

        if (status != NoError || cfString == IntPtr.Zero)
        {
            return $"Input device {deviceId}";
        }

        try
        {
            var buffer = new byte[256];

            return CFStringGetCString(
                cfString,
                buffer,
                buffer.Length,
                CFStringEncodingUtf8)
                ? Encoding.UTF8.GetString(buffer).TrimEnd('\0')
                : $"Input device {deviceId}";
        }
        finally
        {
            CFRelease(cfString);
        }
    }

    public static string GetDeviceUid(uint deviceId)
    {
        var address = new AudioObjectPropertyAddress(
            AudioDevicePropertyDeviceUid,
            AudioObjectPropertyScopeGlobal,
            AudioObjectPropertyElementMain);

        return GetStringProperty(deviceId, address, deviceId.ToString());
    }

    private static bool HasInputStreams(uint deviceId)
    {
        var address = new AudioObjectPropertyAddress(
            AudioDevicePropertyStreams,
            AudioObjectPropertyScopeInput,
            AudioObjectPropertyElementMain);
        var status = AudioObjectGetPropertyDataSize(
            deviceId,
            ref address,
            0,
            IntPtr.Zero,
            out var dataSize);

        return status == NoError && dataSize > 0;
    }

    private static string GetStringProperty(
        uint objectId,
        AudioObjectPropertyAddress address,
        string fallback)
    {
        var dataSize = (uint)IntPtr.Size;
        var status = AudioObjectGetPropertyData(
            objectId,
            ref address,
            0,
            IntPtr.Zero,
            ref dataSize,
            out IntPtr cfString);

        if (status != NoError || cfString == IntPtr.Zero)
        {
            return fallback;
        }

        try
        {
            var buffer = new byte[256];

            return CFStringGetCString(
                cfString,
                buffer,
                buffer.Length,
                CFStringEncodingUtf8)
                ? Encoding.UTF8.GetString(buffer).TrimEnd('\0')
                : fallback;
        }
        finally
        {
            CFRelease(cfString);
        }
    }

    public sealed record MacOsAudioDeviceInfo(string Uid, string Name);

    [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio")]
    private static extern int AudioObjectGetPropertyDataSize(
        uint inObjectId,
        ref AudioObjectPropertyAddress inAddress,
        uint inQualifierDataSize,
        IntPtr inQualifierData,
        out uint outDataSize);

    [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio")]
    private static extern int AudioObjectGetPropertyData(
        uint inObjectId,
        ref AudioObjectPropertyAddress inAddress,
        uint inQualifierDataSize,
        IntPtr inQualifierData,
        ref uint ioDataSize,
        out uint outData);

    [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio", EntryPoint = "AudioObjectGetPropertyData")]
    private static extern int AudioObjectGetPropertyData(
        uint inObjectId,
        ref AudioObjectPropertyAddress inAddress,
        uint inQualifierDataSize,
        IntPtr inQualifierData,
        ref uint ioDataSize,
        [Out] uint[] outData);

    [DllImport("/System/Library/Frameworks/CoreAudio.framework/CoreAudio", EntryPoint = "AudioObjectGetPropertyData")]
    private static extern int AudioObjectGetPropertyData(
        uint inObjectId,
        ref AudioObjectPropertyAddress inAddress,
        uint inQualifierDataSize,
        IntPtr inQualifierData,
        ref uint ioDataSize,
        out IntPtr outData);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFStringGetCString(
        IntPtr theString,
        byte[] buffer,
        int bufferSize,
        uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);
}
