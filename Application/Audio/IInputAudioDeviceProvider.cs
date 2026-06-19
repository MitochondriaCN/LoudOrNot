using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Application.Audio;

public interface IInputAudioDeviceProvider
{
    IInputAudioDevice GetCurrentDefaultInputDevice();
}
