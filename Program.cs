using LoudOrNot.Services;
using LoudOrNot.Services.Abstractions;
using LoudOrNot.Services.Implementations;

namespace LoudOrNot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton<IInputAudioDeviceProvider, SystemDefaultInputAudioDeviceProvider>();
        builder.Services.AddSingleton<IDefaultInputSplSensorProvider, DefaultInputSplSensorProvider>();
        builder.Services.AddSingleton<IAmbientSplService, AmbientSplService>();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
