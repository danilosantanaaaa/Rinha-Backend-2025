namespace Rinha.Api.Models;

public sealed record SummaryItem(
    int TotalRequests,
    decimal TotalAmount);

public sealed record SummaryResponse(
    SummaryItem Default,
    SummaryItem Fallback);