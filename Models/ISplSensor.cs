namespace LoudOrNot.Models;

public interface ISplSensor
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// 测量当前声压级
    /// </summary>
    InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl();
}