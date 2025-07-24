using Microsoft.Extensions.Caching.Distributed;

using Newtonsoft.Json;

using Rinha.Api.Models;

namespace Rinha.Api.Clients;

public class PaymentProcessorClient(
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IDistributedCache _cache = cache;

    public async Task<HttpResponseMessage> PaymentAsync(
        Payment payment,
        PaymentGateway gateway = PaymentGateway.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());
        var result = await client.PostAsJsonAsync("payments", payment, cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to process payment: {result.ReasonPhrase}");
        }

        return result;
    }

    public async Task<HealthResponse> GetHealthAsync(
       PaymentGateway gateway = PaymentGateway.Default,
       CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());

        var healthCached = await _cache.GetStringAsync(
            gateway.ToString(),
            token: cancellationToken);

        HealthResponse health;

        if (!string.IsNullOrEmpty(healthCached))
        {
            return JsonConvert.DeserializeObject<HealthResponse>(healthCached)!;
        }

        try
        {
            health = await client.GetFromJsonAsync<HealthResponse>(
                "payments/service-health",
                cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("Failed to get health response from payment processor");

            await _cache.SetStringAsync(
                gateway.ToString(),
                JsonConvert.SerializeObject(health),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromSeconds(5),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                },
                cancellationToken);

            return health;
        }
        catch (Exception)
        {
            Console.WriteLine($"Failed to get health from {gateway} payment processor. Using fallback.");
            return new HealthResponse(true, 0);
        }
    }

}