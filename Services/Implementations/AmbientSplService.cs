using LoudOrNot.Models;
using LoudOrNot.Services.Abstractions;

namespace LoudOrNot.Services.Implementations;

public sealed class AmbientSplService(IInputAudioDeviceProvider inputAudioDeviceProvider) : IAmbientSplService
{
    private readonly List<InstantaneousAmbientSpl> _historicalInstantaneousAmbientSpls = [];
    private readonly object _historyLock = new();

    public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl()
    {
        var sensor = inputAudioDeviceProvider
            .GetCurrentDefaultInputDevice()
            .GetSplSensor();
        var instantaneousAmbientSpl = sensor.MeasureInstantaneousAmbientSpl();

        lock (_historyLock)
        {
            _historicalInstantaneousAmbientSpls.Add(instantaneousAmbientSpl);
        }

        return instantaneousAmbientSpl;
    }

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSpls()
    {
        lock (_historyLock)
        {
            return [.. _historicalInstantaneousAmbientSpls];
        }
    }

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsAfter(DateTime dateTime)
    {
        lock (_historyLock)
        {
            return _historicalInstantaneousAmbientSpls
                .Where(instantaneousAmbientSpl => instantaneousAmbientSpl.DateTime > dateTime)
                .ToList();
        }
    }

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsBefore(DateTime dateTime)
    {
        lock (_historyLock)
        {
            return _historicalInstantaneousAmbientSpls
                .Where(instantaneousAmbientSpl => instantaneousAmbientSpl.DateTime < dateTime)
                .ToList();
        }
    }

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsBetween(
        DateTime beginDateTime,
        DateTime endDateTime)
    {
        lock (_historyLock)
        {
            return _historicalInstantaneousAmbientSpls
                .Where(instantaneousAmbientSpl =>
                    instantaneousAmbientSpl.DateTime >= beginDateTime &&
                    instantaneousAmbientSpl.DateTime <= endDateTime)
                .ToList();
        }
    }
}
