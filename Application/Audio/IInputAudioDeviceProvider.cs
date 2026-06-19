using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Application.Audio;

public interface IInputAudioDeviceProvider
{
    IReadOnlyList<IInputAudioDevice> GetInputDevices();

    IInputAudioDevice GetCurrentDefaultInputDevice();
}
