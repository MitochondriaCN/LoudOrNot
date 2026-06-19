using System.Diagnostics;
using LoudOrNot.Application.Audio;
using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Infrastructure.Audio.Linux;

public class LinuxInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    private const string DefaultDeviceId = "default";

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
