using System.Diagnostics;

using Rinha.Api.Clients;
using Rinha.Api.Models;

namespace Rinha.Api.Workers;

public class HealthBackgroundService(
    HealthSummary healthSummary,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<HealthBackgroundService> logger) : BackgroundService
{
    private readonly HealthSummary _healthSummary = healthSummary;
    private readonly ILogger<HealthBackgroundService> _logger = logger;
    private readonly PaymentProcessorClient _client =
        serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<PaymentProcessorClient>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var healthDefault = await _client.GetHealthAsync(PaymentGateway.Default);
                var healthFallback = await _client.GetHealthAsync(PaymentGateway.Fallback);

                var @default = new HealthStatus(PaymentGateway.Default, healthDefault);
                var @fallback = new HealthStatus(PaymentGateway.Fallback, healthFallback);

                _healthSummary.Set(@default, fallback);
            }
            catch (Exception ex)
            {
                _healthSummary.Set(
                    new HealthStatus(PaymentGateway.Default, new HealthResponse(false, 0)),
                    new HealthStatus(PaymentGateway.Fallback, new HealthResponse(false, 0)));

                _logger.LogError($"Failed to get health from payment processors: {ex.Message}", ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
