using LoudOrNot.Models;

namespace LoudOrNot.Services.Abstractions;

/// <summary>
/// 环境声压级服务
/// </summary>
public interface IAmbientSplService
{
    InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl();

    List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSpls();

    List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsAfter(DateTime dateTime);

    List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsBefore(DateTime dateTime);

    List<InstantaneousAmbientSpl> GetHistoricalInstantaneousAmbientSplsBetween(DateTime beginDateTime,
        DateTime endDateTime);
}
