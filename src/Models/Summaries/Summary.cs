namespace Rinha.Api.Models.Summaries;

public sealed record Summary(
    string gateway,
    int TotalRequests,
    decimal TotalAmount)
{
    public Summary() : this(string.Empty, default, default) { }
}