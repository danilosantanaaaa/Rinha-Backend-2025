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

    public void SetUpdateHealthy(PaymentGateway gateway, bool failing)
    {
        _health[gateway].Failing = failing;
    }

    public void SetUpdateHealthy(PaymentGateway gateway, HealthResponse health)
    {
        _health[gateway] = health;
    }

    public bool IsDefaultBest()
    {
        var @default = _health[PaymentGateway.Default];
        var fallback = _health[PaymentGateway.Fallback];

        return @default.IsHealthy && !fallback.IsHealthy ||
            @default.IsHealthy && @default.MinResponseTime <= fallback.MinResponseTime;
    }

    public bool IsFallbackBest()
    {
        var @default = _health[PaymentGateway.Default];
        var fallback = _health[PaymentGateway.Fallback];

        return fallback.IsHealthy && !@default.IsHealthy;
    }

    public bool IsBothFailing() =>
        !_health[PaymentGateway.Default].IsHealthy &&
        !_health[PaymentGateway.Fallback].IsHealthy;

    private async Task UpdateHealthyAsync(PaymentGateway gateway, CancellationToken cancellationToken)
    {
        try
        {
            //logger.LogInformation("Get to cache {gateway} in {datetime}", gateway, DateTime.Now);
            var health = await client.GetHealthAsync(gateway, cancellationToken);

            if (health is null)
            {
                return;
            }

            SetUpdateHealthy(gateway, health);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
        }
    }
}