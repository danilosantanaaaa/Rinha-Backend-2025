namespace Rinha.Api.Models;

public sealed record HealthResponse(
    bool Failing,
    int MinResponseTime)
{
    public bool IsHealthy => !Failing;
}