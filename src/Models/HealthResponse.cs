using System.Text.Json.Serialization;

namespace Rinha.Api.Models;

public sealed class HealthResponse
{
    public bool Failing { get; set; }
    public int MinResponseTime { get; set; }

    [JsonIgnore]
    public bool IsLocked { get; set; } = false;

    [JsonIgnore]
    public bool IsHealthy => !Failing;

    public static HealthResponse Default = new HealthResponse(false, 0);
    public static HealthResponse Error => new HealthResponse(true, 0);
    public static HealthResponse Locked => new HealthResponse(true);

    public HealthResponse(bool isLocked)
    {
        IsLocked = isLocked;
    }

    public HealthResponse(bool failing, int minResponseTime)
    {
        Failing = failing;
        MinResponseTime = minResponseTime;
    }

    public HealthResponse()
    { }
}