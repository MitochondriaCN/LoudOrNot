using LoudOrNot.Application.Audio;
using LoudOrNot.Infrastructure.Audio.Linux;
using LoudOrNot.Infrastructure.Audio.MacOs;
using LoudOrNot.Infrastructure.Audio.Windows;
using LoudOrNot.Presentation.Worker;

namespace LoudOrNot.CompositionRoot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();
        if (OperatingSystem.IsMacOS())
            builder.Services.AddSingleton<IInputAudioDeviceProvider, MacOsInputAudioDeviceProvider>();
        else if (OperatingSystem.IsLinux())
            builder.Services.AddSingleton<IInputAudioDeviceProvider, LinuxInputAudioDeviceProvider>();
        else if (OperatingSystem.IsWindows())
            builder.Services.AddSingleton<IInputAudioDeviceProvider, WindowsInputAudioDeviceProvider>();
        else
            throw new PlatformNotSupportedException("当前操作系统不支持自动选择默认输入设备。");
        builder.Services.AddSingleton<IAmbientSplService, AmbientSplService>();

        var host = builder.Build();
        host.Run();
    }
}
