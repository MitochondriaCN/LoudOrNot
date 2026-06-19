using LoudOrNot.Application.Audio;
using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Infrastructure.Audio.Windows;

public class WindowsInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    private const string DefaultDeviceId = "default";

    public IInputAudioDevice GetCurrentDefaultInputDevice()
    {
        return new WindowsInputAudioDevice(
            DefaultDeviceId,
            "System default input device",
            "Windows 当前默认输入设备",
            "Windows");
    }
}
