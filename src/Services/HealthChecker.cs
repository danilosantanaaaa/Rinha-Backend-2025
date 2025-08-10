using System.Net;

namespace Rinha.Api.Services;

public sealed class HealthChecker(
    CacheService cacheService,
    PaymentGatewayClient client,
    ILogger<HealthChecker> logger)
{
    private readonly Dictionary<PaymentGateway, bool> _health = new Dictionary<PaymentGateway, bool>
    {
        { PaymentGateway.Default, true },
        { PaymentGateway.Fallback, true }
    };

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>()
        {
            UpdateDefaultStateAsync(cancellationToken),
            UpdateFallbackState(cancellationToken)
        };

        await Task.WhenAll(tasks);
    }

    private async Task UpdateDefaultStateAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Default Health JOB");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await UpdateHealthyAsync(PaymentGateway.Default, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
            }
        }
    }

    private async Task UpdateFallbackState(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fallback Health JOB");
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await UpdateHealthyAsync(PaymentGateway.Fallback, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
            }
        }
    }

    private async Task UpdateHealthyAsync(PaymentGateway gateway, CancellationToken cancellationToken)
    {
        var lastPayment = await cacheService.GetAsync<Payment>($"payment:last:{gateway}", cancellationToken);

        if (lastPayment is not null)
        {
            var result = await client.GetHealthAsync(
                lastPayment,
                gateway,
                cancellationToken);

            SetUpdateHealth(gateway, result.StatusCode == HttpStatusCode.UnprocessableEntity, cancellationToken);
        }
    }

    public bool IsDefaultHealth => _health[PaymentGateway.Default];

    public bool IsFallbackHealth => _health[PaymentGateway.Fallback];

    public void SetUpdateHealth(PaymentGateway gateway, bool status, CancellationToken cancellationToken)
    {
        _health[gateway] = status;
    }
}