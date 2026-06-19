using LoudOrNot.Models;

namespace LoudOrNot.Services.Implementations.MacOs;

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
        throw new NotSupportedException(
            $"当前默认输入设备已解析为 \"{Name}\"，但尚未配置 {Platform} 的麦克风采样后端。");
    }
}
