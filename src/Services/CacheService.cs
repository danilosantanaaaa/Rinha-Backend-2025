using Microsoft.Extensions.Caching.Distributed;

using Newtonsoft.Json;

namespace Rinha.Api.Services;

public class CacheService(IDistributedCache cache)
{
    private readonly IDistributedCache _cache = cache;

    public async Task SetAsync<TItem>(
        string key,
        TItem item,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    where TItem : class
    {
        await _cache.SetStringAsync(
               key.ToString(),
               JsonConvert.SerializeObject(item),
               options ?? new DistributedCacheEntryOptions(),
               cancellationToken);
    }

    public async Task<TItem?> GetAsync<TItem>(string key, CancellationToken cancellationToken = default)
        where TItem : class
    {
        string? result = await _cache.GetStringAsync(
            key,
            token: cancellationToken);

        if (string.IsNullOrEmpty(result))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<TItem?>(result!);
    }
}