using LoudOrNot.Models;

namespace LoudOrNot.Services.Abstractions;

public interface IInputAudioDeviceProvider
{
    InputAudioDevice GetCurrentDefaultInputDevice();
}
