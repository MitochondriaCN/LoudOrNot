namespace LoudOrNot.Models;

public interface IInputAudioDevice
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Platform { get; }

    ISplSensor GetSplSensor();
}