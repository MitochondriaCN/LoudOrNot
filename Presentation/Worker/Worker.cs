using LoudOrNot.Application.Audio;

namespace LoudOrNot.Presentation.Worker;

public class Worker(
    ILogger<Worker> logger,
    IAmbientSplService ambientSplService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var ambientSpl = ambientSplService.MeasureInstantaneousAmbientSpl();

                logger.LogInformation(
                    "Ambient SPL measured at {time}: {spl:F2} dB by {sensor}",
                    ambientSpl.DateTime,
                    ambientSpl.Spl,
                    ambientSpl.Sensor.Name);
            }
            catch (NotSupportedException exception)
            {
                logger.LogWarning(exception, "当前平台尚未配置可用的声压级采样后端。");
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "声压级采样失败。");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
