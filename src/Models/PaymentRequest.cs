namespace Rinha.Api.Models;

public sealed record PaymentRequest(
    Guid CorrelationId,
    decimal Amount);