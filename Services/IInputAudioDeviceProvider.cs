using LoudOrNot.Models;

namespace LoudOrNot.Services;

public interface IInputAudioDeviceProvider
{
    InputAudioDevice GetCurrentDefaultInputDevice();
}
