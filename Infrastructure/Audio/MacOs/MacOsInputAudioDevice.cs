using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Infrastructure.Audio.MacOs;

public class MacOsInputAudioDevice(
    string id,
    string name,
    string description,
    string platform) : IInputAudioDevice
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Platform { get; } = platform;

    public ISplSensor GetSplSensor()
    {
        return new MacOsAudioQueueSplSensor(Id, Name);
    }
}
