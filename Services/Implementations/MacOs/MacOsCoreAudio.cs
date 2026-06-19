using System.Runtime.InteropServices;
using System.Text;

namespace LoudOrNot.Services.Implementations.MacOs;

internal static class MacOsCoreAudio
{
    private const uint AudioObjectSystemObject = 1;
    private const uint AudioHardwarePropertyDefaultInputDevice = 0x64496E20;
    private const uint AudioObjectPropertyName = 0x6C6E616D;
    private const uint AudioObjectPropertyScopeGlobal = 0x676C6F62;
    private const uint AudioObjectPropertyElementMain = 0;
    private const uint CFStringEncodingUtf8 = 0x08000100;
    private const int NoError = 0;

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
            throw new InvalidOperationException("无法读取 macOS 当前默认输入设备。");
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
