using LoudOrNot.Models;

namespace LoudOrNot.Services;

public sealed class DefaultInputSplSensorProvider(IInputAudioDeviceProvider inputAudioDeviceProvider)
    : IDefaultInputSplSensorProvider
{
    public ISplSensor GetCurrentDefaultInputSensor()
    {
        var currentDefaultInputDevice = inputAudioDeviceProvider.GetCurrentDefaultInputDevice();

        return new SystemDefaultInputSplSensor(currentDefaultInputDevice);
    }
}
