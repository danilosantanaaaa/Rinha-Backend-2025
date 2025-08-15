using System.Text.Json.Serialization;

namespace Rinha.Api.Models.Healths;

public sealed class HealthResponse
{
    public bool Failing { get; set; }
    public int MinResponseTime { get; set; }

    [JsonIgnore]
    public bool IsHealthy => !Failing;

    public static HealthResponse Default = new HealthResponse(false, 0);
    public static HealthResponse Error => new HealthResponse(true, 0);
    public HealthResponse(bool failing, int minResponseTime)
    {
        Failing = failing;
        MinResponseTime = minResponseTime;
    }

    public HealthResponse()
    { }
}