namespace Rinha.Api.Models;

public record Summary(
    PaymentGateway Type,
    int TotalRequests,
    decimal TotalAmount)
{
    public Summary() : this(default, default, default) { }
}