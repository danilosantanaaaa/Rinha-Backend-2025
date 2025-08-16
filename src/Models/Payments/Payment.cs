using System.Text.Json.Serialization;

namespace Rinha.Api.Models.Payments;

public sealed class Payment
{
    public Payment(Guid correlationId, decimal amount, DateTime requestedAt)
    {
        CorrelationId = correlationId;
        Amount = amount;
        RequestedAt = requestedAt;
    }

    public Guid CorrelationId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; }

    [JsonIgnore]
    public PaymentGateway Gateway { get; set; }
}