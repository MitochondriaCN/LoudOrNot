namespace LoudOrNot.Models;

public sealed class SystemDefaultInputSplSensor(InputAudioDevice inputAudioDevice) : ISplSensor
{
    public string Name => inputAudioDevice.Name;

    public string Description => inputAudioDevice.Description;

    public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl()
    {
        throw new NotSupportedException(
            $"当前默认输入设备已解析为 \"{inputAudioDevice.Name}\"，但尚未配置 {inputAudioDevice.Platform} 的麦克风采样后端。");
    }
}
