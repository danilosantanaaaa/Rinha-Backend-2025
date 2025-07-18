namespace Rinha.Api.Models;

public record Summary(
    PaymentProcessorType Type,
    int TotalRequests,
    decimal TotalAmount)
{
    public Summary() : this(default, default, default) { }
}