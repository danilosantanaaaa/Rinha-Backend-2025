namespace Rinha.Api.Models.Payments;

public sealed record PaymentRequest(
    Guid CorrelationId,
    decimal Amount);