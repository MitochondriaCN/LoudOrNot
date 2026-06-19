using System.Diagnostics;
using LoudOrNot.Models;
using LoudOrNot.Services.Abstractions;

namespace LoudOrNot.Services.Implementations;

public sealed class SystemDefaultInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    private const string DefaultDeviceId = "default";

    public InputAudioDevice GetCurrentDefaultInputDevice()
    {
        if (OperatingSystem.IsMacOS())
        {
            return GetMacOsCurrentDefaultInputDevice();
        }

        if (OperatingSystem.IsLinux())
        {
            return GetLinuxCurrentDefaultInputDevice();
        }

        if (OperatingSystem.IsWindows())
        {
            return GetWindowsCurrentDefaultInputDevice();
        }

        throw new PlatformNotSupportedException("当前操作系统不支持自动选择默认输入设备。");
    }

    private static InputAudioDevice GetMacOsCurrentDefaultInputDevice()
    {
        var deviceId = MacOsCoreAudio.GetDefaultInputDeviceId();
        var deviceName = MacOsCoreAudio.GetDeviceName(deviceId);

        return new InputAudioDevice(
            deviceId.ToString(),
            deviceName,
            "macOS 当前默认输入设备",
            "macOS");
    }

    private static InputAudioDevice GetLinuxCurrentDefaultInputDevice()
    {
        var defaultSource = TryRunProcess("pactl", "get-default-source");

        if (!string.IsNullOrWhiteSpace(defaultSource))
        {
            var sourceName = defaultSource.Trim();

            return new InputAudioDevice(
                sourceName,
                sourceName,
                "PulseAudio/PipeWire 当前默认输入源",
                "Linux");
        }

        return new InputAudioDevice(
            DefaultDeviceId,
            "System default input device",
            "Linux 当前默认输入设备",
            "Linux");
    }

    private static InputAudioDevice GetWindowsCurrentDefaultInputDevice()
    {
        return new InputAudioDevice(
            DefaultDeviceId,
            "System default input device",
            "Windows 当前默认输入设备",
            "Windows");
    }

    private static string? TryRunProcess(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();

            return process.WaitForExit(1000) && process.ExitCode == 0
                ? output
                : null;
        }
        catch
        {
            return null;
        }
    }
}
