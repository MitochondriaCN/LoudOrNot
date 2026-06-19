namespace LoudOrNot.Models;

public record InstantaneousAmbientSpl(
    DateTime DateTime,
    Double Spl,
    ISplSensor Sensor
);