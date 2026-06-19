using System.Diagnostics;
using LoudOrNot.Models;
using LoudOrNot.Services.Abstractions;

namespace LoudOrNot.Services.Implementations;

public sealed class SystemDefaultInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    private const string DefaultDeviceId = "default";

    public IInputAudioDevice GetCurrentDefaultInputDevice()
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

    private static IInputAudioDevice GetMacOsCurrentDefaultInputDevice()
    {
        var deviceId = MacOsCoreAudio.GetDefaultInputDeviceId();
        var deviceName = MacOsCoreAudio.GetDeviceName(deviceId);

        return CreateInputAudioDevice(
            deviceId.ToString(),
            deviceName,
            "macOS 当前默认输入设备",
            "macOS");
    }

    private static IInputAudioDevice GetLinuxCurrentDefaultInputDevice()
    {
        var defaultSource = TryRunProcess("pactl", "get-default-source");

        if (!string.IsNullOrWhiteSpace(defaultSource))
        {
            var sourceName = defaultSource.Trim();

            return CreateInputAudioDevice(
                sourceName,
                sourceName,
                "PulseAudio/PipeWire 当前默认输入源",
                "Linux");
        }

        return CreateInputAudioDevice(
            DefaultDeviceId,
            "System default input device",
            "Linux 当前默认输入设备",
            "Linux");
    }

    private static IInputAudioDevice GetWindowsCurrentDefaultInputDevice()
    {
        return CreateInputAudioDevice(
            DefaultDeviceId,
            "System default input device",
            "Windows 当前默认输入设备",
            "Windows");
    }

    private static IInputAudioDevice CreateInputAudioDevice(
        string id,
        string name,
        string description,
        string platform)
    {
        return new SystemInputAudioDevice(id, name, description, platform);
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

    private sealed record SystemInputAudioDevice(
        string Id,
        string Name,
        string Description,
        string Platform) : IInputAudioDevice
    {
        public ISplSensor GetSplSensor()
        {
            return new UnsupportedInputSplSensor(Name, Description, Platform);
        }
    }

    private sealed class UnsupportedInputSplSensor(
        string inputAudioDeviceName,
        string inputAudioDeviceDescription,
        string inputAudioDevicePlatform) : ISplSensor
    {
        public string Name => inputAudioDeviceName;

        public string Description => inputAudioDeviceDescription;

        public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl()
        {
            throw new NotSupportedException(
                $"当前默认输入设备已解析为 \"{inputAudioDeviceName}\"，但尚未配置 {inputAudioDevicePlatform} 的麦克风采样后端。");
        }
    }
}
