using LoudOrNot.Models;

namespace LoudOrNot.Services.Abstractions;

public interface IInputAudioDeviceProvider
{
    IInputAudioDevice GetCurrentDefaultInputDevice();
}
