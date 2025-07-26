using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class HealthBackgroundService(
    HealthSummary healthSummary,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<HealthBackgroundService> logger) : BackgroundService
{
    private readonly HealthSummary _healthSummary = healthSummary;
    private readonly ILogger<HealthBackgroundService> _logger = logger;
    private readonly PaymentGatewayClient _client =
        serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<PaymentGatewayClient>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>()
        {
            GetHealthDefault(stoppingToken),
            GetHealthFallback(stoppingToken)
        };

        await Task.WhenAll(tasks);
    }

    private async Task GetHealthDefault(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check payment processor default");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var @default = await _client.GetHealthAsync(
                    PaymentGateway.Default,
                    stoppingToken);

                _healthSummary.SetDefault(@default);
            }
            catch (Exception ex)
            {
                _healthSummary.SetDefault(
                    new HealthResponse(false, 0));

                _logger.LogError($"Failed to get health from payment processors: {ex.Message}", ex);
            }

            SetBestGateway();
        }
    }

    private async Task GetHealthFallback(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check payment processor fallback");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var fallback = await _client.GetHealthAsync(
                    PaymentGateway.Fallback,
                    stoppingToken);

                _healthSummary.SetFallback(fallback);
            }
            catch (Exception ex)
            {
                _healthSummary.SetFallback(
                   new HealthResponse(false, 0));

                _logger.LogError($"Failed to get health from payment processors: {ex.Message}", ex);
            }

            SetBestGateway();
        }
    }

    private void SetBestGateway()
    {
        HealthResponse def = _healthSummary.Default;
        HealthResponse fal = _healthSummary.Fallback;

        // Verificando se ambos os servidores estão com problemas
        if (_healthSummary.IsBothGatewaysUnhealthy())
        {
            PaymentService.Gateway = PaymentGateway.Default;
            return;
        }

        if (def.IsHealthy && def.MinResponseTime <= 100)
        {
            PaymentService.Gateway = PaymentGateway.Default;
            return;
        }

        // Verificando se o servidor fallback é mais rápido que o default
        if (def.IsHealthy && fal.IsHealthy && def.MinResponseTime > fal.MinResponseTime)
        {
            PaymentService.Gateway = PaymentGateway.Fallback;
            return;
        }

        if (def.IsHealthy)
        {
            PaymentService.Gateway = PaymentGateway.Default;
            return;
        }

        PaymentService.Gateway = PaymentGateway.Default;
    }
}
