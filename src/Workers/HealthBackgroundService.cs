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
            Task.Run(() => GetHealthDefault(stoppingToken)),
            Task.Run(() => GetHealthFallback(stoppingToken))
        };

        await Task.WhenAll(tasks);
    }

    private async Task GetHealthDefault(CancellationToken stoppingToken)
    {
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
        }
    }

    private async Task GetHealthFallback(CancellationToken stoppingToken)
    {
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

            //await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
