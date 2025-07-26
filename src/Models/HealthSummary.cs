namespace Rinha.Api.Models;

public record HealthSummary
{
    public HealthResponse Default { get; private set; } = new HealthResponse(false, 0);
    public HealthResponse Fallback { get; private set; } = new HealthResponse(false, 0);
    public static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 2);

    public void SetDefault(
        HealthResponse @default)
    {
        Default = @default;
    }

    public void SetFallback(
        HealthResponse fallback)
    {
        Fallback = fallback;
    }

    public bool IsBothGatewaysUnhealthy()
    {
        return !Default.IsHealthy && !Fallback.IsHealthy;
    }

    public bool IsAnyGatewayHealthy() =>
        !IsBothGatewaysUnhealthy();
}