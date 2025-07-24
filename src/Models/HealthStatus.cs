namespace Rinha.Api.Models;

public record HealthStatus(
    PaymentGateway Default,
    HealthResponse HealthResponse);

public record HealthSummary
{
    public HealthStatus Default { get; private set; } = new(PaymentGateway.Default, new HealthResponse(false, 0));
    public HealthStatus Fallback { get; private set; } = new(PaymentGateway.Fallback, new HealthResponse(false, 0));

    public void Set(
        HealthStatus @default,
        HealthStatus fallback)
    {
        Default = @default;
        Fallback = fallback;
    }
}