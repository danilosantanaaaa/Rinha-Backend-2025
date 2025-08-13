using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class HealthBackgroundService(
    HealthChecker healthChecker) : BackgroundService
{
    private readonly HealthChecker _healthChecker = healthChecker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) =>
         await _healthChecker.ExecuteAsync(stoppingToken);
}
