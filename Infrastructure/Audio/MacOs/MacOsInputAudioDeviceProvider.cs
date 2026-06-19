using LoudOrNot.Application.Audio;
using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Infrastructure.Audio.MacOs;

public class MacOsInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    public IInputAudioDevice GetCurrentDefaultInputDevice()
    {
        var deviceId = MacOsCoreAudio.GetDefaultInputDeviceId();
        var deviceName = MacOsCoreAudio.GetDeviceName(deviceId);

        return new MacOsInputAudioDevice(
            deviceId.ToString(),
            deviceName,
            "macOS 当前默认输入设备",
            "macOS");
    }
}