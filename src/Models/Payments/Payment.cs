using System.Text.Json.Serialization;

namespace Rinha.Api.Models.Payments;

public sealed record Payment(
    Guid CorrelationId,
    decimal Amount,
    DateTime RequestedAt)
{
    [JsonIgnore]
    public PaymentGateway Gateway { get; set; }
}