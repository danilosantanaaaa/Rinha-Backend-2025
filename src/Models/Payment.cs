namespace Rinha.Api.Models;

public sealed record Payment(
    Guid CorrelationId,
    decimal Amount,
    DateTime RequestedAt);