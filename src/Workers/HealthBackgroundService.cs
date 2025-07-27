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
        while (!stoppingToken.IsCancellationRequested)
        {

            try
            {
                var tasks = new List<Task>()
                {
                    GetHealthOnlineAsync(PaymentGateway.Default, _healthSummary.SetDefault ,stoppingToken),
                    GetHealthOnlineAsync(PaymentGateway.Fallback, _healthSummary.SetFallback ,stoppingToken)
                };

                await Task.WhenAll(tasks);

                _healthSummary.SetBestGateway();
            }
            catch (Exception)
            {

            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
    }

    private async Task GetHealthOnlineAsync(
        PaymentGateway gateway,
        Action<HealthResponse> set,
        CancellationToken stoppingToken)
    {
        try
        {
            var response = await _client.GetHealthAsync(
                gateway,
                stoppingToken);

            set(response);
        }
        catch (Exception ex)
        {
            set(new HealthResponse(false, 0));

            _logger.LogError($"Failed to get health from payment processors: {ex.Message} in {gateway}", ex);
        }
    }
}
