namespace Rinha.Api.Models;

public record Summary(
    PaymentGateway gateway,
    int TotalRequests,
    decimal TotalAmount)
{
    public Summary() : this(default, default, default) { }
}