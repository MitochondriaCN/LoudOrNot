using LoudOrNot.Services.Abstractions;
using LoudOrNot.Services.Implementations;

namespace LoudOrNot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();
        builder.Services.AddSingleton<IInputAudioDeviceProvider, SystemDefaultInputAudioDeviceProvider>();
        builder.Services.AddSingleton<IAmbientSplService, AmbientSplService>();

        var host = builder.Build();
        host.Run();
    }
}
