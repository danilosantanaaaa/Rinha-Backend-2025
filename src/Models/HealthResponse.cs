namespace Rinha.Api.Models;

public record HealthResponse(
    bool Failing,
    int MinResponseTime)
{
    public bool IsHealthy => !Failing;
}