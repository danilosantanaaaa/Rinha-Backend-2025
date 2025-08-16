using Rinha.Api.Models.Healths;

namespace Rinha.Api.Services;

public sealed class HealthChecker(
    PaymentClient client,
    ILogger<HealthChecker> logger)
{
    private readonly Dictionary<PaymentGateway, HealthResponse> _health = new Dictionary<PaymentGateway, HealthResponse>
    {
        { PaymentGateway.Default,  HealthResponse.Default },
        { PaymentGateway.Fallback, HealthResponse.Default }
    };

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            UpdateHealthyAsync(PaymentGateway.Default, cancellationToken),
            UpdateHealthyAsync(PaymentGateway.Fallback, cancellationToken));
    }

    public void UpdateHealth(PaymentGateway gateway, bool failing)
    {
        logger.LogInformation("PaymentProcess Failing: {0}", gateway);
        _health[gateway].Failing = failing;
    }

    public void UpdateHealth(PaymentGateway gateway, HealthResponse health)
    {
        logger.LogInformation("PaymentProcess Failing: {0} {1}", gateway, health);
        _health[gateway] = health;
    }

    public bool IsDefaultBest()
    {
        var @default = _health[PaymentGateway.Default];
        var fallback = _health[PaymentGateway.Fallback];

        if (@default.IsHealthy && !fallback.IsHealthy)
        {
            return true;
        }

        return
            @default.IsHealthy && fallback.IsHealthy && @default.MinResponseTime <= fallback.MinResponseTime;
    }

    public bool IsBothFailing() =>
        !_health[PaymentGateway.Default].IsHealthy &&
        !_health[PaymentGateway.Fallback].IsHealthy;

    private async Task UpdateHealthyAsync(PaymentGateway gateway, CancellationToken cancellationToken)
    {
        try
        {
            var health = await client.GetHealthAsync(gateway, cancellationToken);

            // Significa que foi bloqueado pela outra api ou deu erro
            if (health is null)
            {
                return;
            }
        
            UpdateHealth(gateway, health);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
        }
    }
}