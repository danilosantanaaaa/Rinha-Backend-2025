using System.Text.Json;

using Rinha.Api.Models.Healths;

namespace Rinha.Api.Repositories;

public sealed class PaymentCacheRepository(IConnectionMultiplexer redis)
{
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task SetHealthAsync(
        string key,
        HealthResponse value,
        TimeSpan? expiry = null)
    {
        var db = _redis.GetDatabase();

        var json = JsonSerializer.Serialize(value, AppJsonSerializerContext.Default.HealthResponse);

        await db.StringSetAsync(key, json, expiry);
    }

    public async Task<HealthResponse?> GetHealthAsync(string key)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(key);

        return json.HasValue
            ? JsonSerializer.Deserialize(json!, AppJsonSerializerContext.Default.HealthResponse)
            : default;
    }
}