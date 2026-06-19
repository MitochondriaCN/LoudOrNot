using LoudOrNot.Application.Audio;
using LoudOrNot.Domain.Audio;
using LoudOrNot.Presentation.Worker;
using Microsoft.Extensions.Logging;

var logger = new CapturingLogger<Worker>();
var ambientSplService = new FakeAmbientSplService();
var worker = new Worker(logger, ambientSplService);

await worker.StartAsync(CancellationToken.None);
await Task.Delay(TimeSpan.FromMilliseconds(2500));
await worker.StopAsync(CancellationToken.None);

Console.WriteLine("Captured measurements:");
foreach (var message in logger.Messages)
{
    Console.WriteLine(message);
}

if (ambientSplService.MeasurementCount < 3)
{
    Console.Error.WriteLine(
        $"Expected at least 3 measurements in 2.5 seconds, got {ambientSplService.MeasurementCount}.");
    return 1;
}

Console.WriteLine($"OK: measured {ambientSplService.MeasurementCount} times.");
return 0;

internal sealed class FakeAmbientSplService : IAmbientSplService
{
    private readonly FakeSplSensor _sensor = new();
    private readonly List<InstantaneousAmbientSpl> _measurements = [];

    public int MeasurementCount => _measurements.Count;

    public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl()
    {
        var measurement = new InstantaneousAmbientSpl(
            DateTime.UtcNow,
            42 + _measurements.Count,
            _sensor);

        _measurements.Add(measurement);
        return measurement;
    }

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSpls() => [.. _measurements];

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsAfter(DateTime dateTime) =>
        _measurements.Where(measurement => measurement.DateTime > dateTime).ToList();

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsBefore(DateTime dateTime) =>
        _measurements.Where(measurement => measurement.DateTime < dateTime).ToList();

    public List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsBetween(
        DateTime beginDateTime,
        DateTime endDateTime) =>
        _measurements
            .Where(measurement => measurement.DateTime >= beginDateTime && measurement.DateTime <= endDateTime)
            .ToList();
}

internal sealed class FakeSplSensor : ISplSensor
{
    public string Name => "Fake SPL Sensor";
    public string Description => "Deterministic sensor for worker smoke tests.";

    public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl() =>
        new(DateTime.UtcNow, 42, this);
}

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }
}
