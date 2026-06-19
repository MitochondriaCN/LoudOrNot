using LoudOrNot.Models;

namespace LoudOrNot.Services;

/// <summary>
/// 当前默认输入设备的声压级传感器提供器
/// </summary>
public interface IDefaultInputSplSensorProvider
{
    ISplSensor GetCurrentDefaultInputSensor();
}
