namespace Rinha.Api.Models;

public record PaymentRequest(
    Guid CorrelationId,
    decimal Amount);