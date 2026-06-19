using LoudOrNot.Models;
using LoudOrNot.Services.Abstractions;

namespace LoudOrNot.Services.Implementations;

public sealed class DefaultInputSplSensorProvider(IInputAudioDeviceProvider inputAudioDeviceProvider)
    : IDefaultInputSplSensorProvider
{
    public ISplSensor GetCurrentDefaultInputSensor()
    {
        var currentDefaultInputDevice = inputAudioDeviceProvider.GetCurrentDefaultInputDevice();

        return new SystemDefaultInputSplSensor(currentDefaultInputDevice);
    }
}
