using LoudOrNot.Models;
using LoudOrNot.Services.Abstractions;

namespace LoudOrNot.Services.Implementations.Windows;

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
