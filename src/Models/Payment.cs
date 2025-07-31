namespace Rinha.Api.Models;

public record Payment(
    Guid CorrelationId,
    decimal Amount,
    DateTime RequestedAt);