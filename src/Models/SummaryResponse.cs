namespace Rinha.Api.Models;

public record SummaryItem(
    int TotalRequests,
    decimal TotalAmount);

public record SummaryResponse(
    SummaryItem Default,
    SummaryItem Fallback);