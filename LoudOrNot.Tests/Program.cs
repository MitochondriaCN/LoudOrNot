using LoudOrNot.Application.Audio;
using LoudOrNot.Infrastructure.Audio.Linux;
using LoudOrNot.Infrastructure.Audio.MacOs;
using LoudOrNot.Infrastructure.Audio.Windows;

var inputAudioDeviceProvider = CreateInputAudioDeviceProvider();
try
{
    var inputAudioDevice = inputAudioDeviceProvider.GetCurrentDefaultInputDevice();
    var ambientSplService = new AmbientSplService(inputAudioDeviceProvider);

    Console.WriteLine($"Using input device: {inputAudioDevice.Name} ({inputAudioDevice.Platform})");

    for (var index = 0; index < 5; index++)
    {
        var ambientSpl = ambientSplService.MeasureInstantaneousAmbientSpl();

        Console.WriteLine(
            "{0:O} {1:F2} dB by {2}",
            ambientSpl.DateTime,
            ambientSpl.Spl,
            ambientSpl.Sensor.Name);

        if (index < 4)
            await Task.Delay(TimeSpan.FromSeconds(1));
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Real audio input measurement failed: {exception.Message}");
    return 1;
}

static IInputAudioDeviceProvider CreateInputAudioDeviceProvider()
{
    if (OperatingSystem.IsMacOS())
        return new MacOsInputAudioDeviceProvider();
    if (OperatingSystem.IsLinux())
        return new LinuxInputAudioDeviceProvider();
    if (OperatingSystem.IsWindows())
        return new WindowsInputAudioDeviceProvider();

    throw new PlatformNotSupportedException("当前操作系统不支持自动选择默认输入设备。");
}
