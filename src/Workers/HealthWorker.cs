using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class HealthWorker(
    ILogger<HealthWorker> logger,
    HealthChecker healthChecker) : BackgroundService
{
    private readonly ILogger<HealthWorker> _logger = logger;
    private readonly HealthChecker _healthChecker = healthChecker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await _healthChecker.ExecuteAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                await Task.Delay(TimeSpan.FromMilliseconds(1000), stoppingToken);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMilliseconds(5000), stoppingToken);
            }

        }
    }
}
