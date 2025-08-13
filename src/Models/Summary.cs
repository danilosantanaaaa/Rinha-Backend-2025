namespace Rinha.Api.Models;

public sealed record Summary(
    string gateway,
    int TotalRequests,
    decimal TotalAmount)
{
    public Summary() : this(string.Empty, default, default) { }
}