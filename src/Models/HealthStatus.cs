namespace Rinha.Api.Models;

public class HealthStatus
{
    public PaymentProcessorType Default { get; set; }
    public PaymentProcessorType Fallback { get; set; }
    public bool Failing { get; set; }
    public int MinResponseTime { get; set; }

    public void Set(
        PaymentProcessorType @default,
        PaymentProcessorType fallback)
    {
        Default = @default;
        Fallback = fallback;
    }
}