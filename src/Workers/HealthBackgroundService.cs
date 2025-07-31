using System.Net;

using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class HealthBackgroundService(
    ILogger<HealthBackgroundService> logger,
    CacheService cacheService,
    HealthChecker healthChecker) : BackgroundService
{
    private readonly ILogger<HealthBackgroundService> _logger = logger;
    private readonly CacheService _cacheService = cacheService;
    private readonly HealthChecker _healthChecker = healthChecker;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tasks = new List<Task>()
                {
                    GetHealthOnlineAsync(
                        PaymentGateway.Default,
                        _healthChecker.SetDefault ,
                        stoppingToken),

                    GetHealthOnlineAsync(
                        PaymentGateway.Fallback,
                        _healthChecker.SetFallback,
                        stoppingToken)
                };

                await Task.WhenAll(tasks);
                UpdateHealth();

            }
            catch (Exception)
            {

            }

            await Task.Delay(TimeSpan.FromMilliseconds(15));
        }
    }

    private async Task GetHealthOnlineAsync(
        PaymentGateway gateway,
        Action<bool> set,
        CancellationToken stoppingToken)
    {
        try
        {
            var lastPayment = await _cacheService.GetAsync<Payment>($"payment:last:{gateway}", stoppingToken);

            if (lastPayment is not null)
            {
                var response = gateway == PaymentGateway.Default
                    ? await _healthChecker.CloseAsync(lastPayment)
                    : await _healthChecker.HalfOpenAsync(lastPayment);

                bool available = response.StatusCode == HttpStatusCode.UnprocessableEntity;
                set(available);
            }
        }
        catch (Exception ex)
        {
            set(false);
            _logger.LogError($"Failed to get health from payment processors: {ex.Message} in {gateway}", ex);
        }
    }

    public void UpdateHealth()
    {
        if (_healthChecker.DefaultOnline)
        {
            _healthChecker.State = CircuitBreakerState.Close;
        }
        else if (_healthChecker.FallbackOnline)
        {
            _healthChecker.State = CircuitBreakerState.HalfOpen;
        }
        else
        {
            _healthChecker.State = CircuitBreakerState.Open;
        }
    }
}
