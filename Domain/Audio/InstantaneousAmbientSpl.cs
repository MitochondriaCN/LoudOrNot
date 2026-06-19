namespace LoudOrNot.Domain.Audio;

public record InstantaneousAmbientSpl(
    DateTime DateTime,
    Double Spl,
    ISplSensor Sensor
);