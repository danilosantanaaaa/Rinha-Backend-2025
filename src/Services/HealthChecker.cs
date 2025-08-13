namespace Rinha.Api.Services;

public sealed class HealthChecker(
    PaymentClient client,
    ILogger<HealthChecker> logger)
{
    private readonly Dictionary<PaymentGateway, HealthResponse> _health = new Dictionary<PaymentGateway, HealthResponse>
    {
        { PaymentGateway.Default,  HealthResponse.Default},
        { PaymentGateway.Fallback, HealthResponse.Default }
    };

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                var tasks = new List<Task>()
                {
                    UpdateHealthyAsync(PaymentGateway.Default, cancellationToken),
                    UpdateHealthyAsync(PaymentGateway.Fallback, cancellationToken)
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
            }
        }

    }

    public void SetUpdateHealthy(PaymentGateway gateway, bool failing)
    {
        _health[gateway].Failing = failing;
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

    private async Task UpdateHealthyAsync(PaymentGateway gateway, CancellationToken cancellationToken)
    {
        try
        {
            //logger.LogInformation("Get to cache {gateway} in {datetime}", gateway, DateTime.Now);
            var health = await client.GetHealthAsync(gateway, cancellationToken);

            if (health.IsLocked)
            {
                return;
            }

            _health[gateway] = health;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
        }
    }
}