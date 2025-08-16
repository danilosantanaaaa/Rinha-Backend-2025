using System.Text.Json.Serialization;

namespace Rinha.Api.Models.Healths;

public sealed class HealthResponse
{
    [JsonPropertyName("failing")]
    public bool Failing { get; set; }

    [JsonPropertyName("minResponseTime")]
    public int MinResponseTime { get; set; }

    [JsonIgnore]
    public bool IsHealthy => !Failing;

    [JsonIgnore]
    public static HealthResponse Default = new HealthResponse(false, 0);

    public HealthResponse(bool failing, int minResponseTime)
    {
        Failing = failing;
        MinResponseTime = minResponseTime;
    }

    [JsonConstructor]
    public HealthResponse()
    { }
}