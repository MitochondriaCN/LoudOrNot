using System.Diagnostics;
using LoudOrNot.Application.Audio;
using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Infrastructure.Audio.Linux;

public class LinuxInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    private const string DefaultDeviceId = "default";

    public IReadOnlyList<IInputAudioDevice> GetInputDevices()
    {
        var sources = TryRunProcess("pactl", "list short sources");
        if (!string.IsNullOrWhiteSpace(sources))
        {
            var devices = sources
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(columns => columns.Length >= 2)
                .Select(columns => new LinuxInputAudioDevice(
                    columns[1],
                    columns[1],
                    "PulseAudio/PipeWire 输入源",
                    "Linux"))
                .Cast<IInputAudioDevice>()
                .ToList();

            if (devices.Count > 0)
            {
                return devices;
            }
        }

        return [GetCurrentDefaultInputDevice()];
    }

    public IInputAudioDevice GetCurrentDefaultInputDevice()
    {
        var defaultSource = TryRunProcess("pactl", "get-default-source");

        if (!string.IsNullOrWhiteSpace(defaultSource))
        {
            var sourceName = defaultSource.Trim();

            return new LinuxInputAudioDevice(
                sourceName,
                sourceName,
                "PulseAudio/PipeWire 当前默认输入源",
                "Linux");
        }

        return new LinuxInputAudioDevice(
            DefaultDeviceId,
            "System default input device",
            "Linux 当前默认输入设备",
            "Linux");
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
