using System.Text.Json;

using StackExchange.Redis;

namespace Rinha.Api.Services;

public sealed class CacheService(IConnectionMultiplexer redis)
{
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task SetAsync<TValue>(
        string key,
        TValue value,
        TimeSpan? expiry = null
    )
    where TValue : class
    {
        var db = _redis.GetDatabase();

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };

        var json = JsonSerializer.Serialize(value, options);

        await db.StringSetAsync(key, json, expiry);
    }

    public async Task<TValue?> GetAsync<TValue>(string key)
        where TValue : class
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(key);

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default
        };

        return json.HasValue
            ? JsonSerializer.Deserialize<TValue>(json, options)
            : default;
    }
}