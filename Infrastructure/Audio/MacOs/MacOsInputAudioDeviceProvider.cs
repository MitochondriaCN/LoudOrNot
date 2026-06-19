using LoudOrNot.Application.Audio;
using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Infrastructure.Audio.MacOs;

public class MacOsInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    public IReadOnlyList<IInputAudioDevice> GetInputDevices()
    {
        return MacOsCoreAudio.GetInputDevices()
            .Select(device => new MacOsInputAudioDevice(
                device.Uid,
                device.Name,
                "macOS 输入设备",
                "macOS"))
            .ToList();
    }

    public IInputAudioDevice GetCurrentDefaultInputDevice()
    {
        var deviceId = MacOsCoreAudio.GetDefaultInputDeviceId();
        var deviceName = MacOsCoreAudio.GetDeviceName(deviceId);
        var deviceUid = MacOsCoreAudio.GetDeviceUid(deviceId);

        return new MacOsInputAudioDevice(
            deviceUid,
            deviceName,
            "macOS 当前默认输入设备",
            "macOS");
    }
}
