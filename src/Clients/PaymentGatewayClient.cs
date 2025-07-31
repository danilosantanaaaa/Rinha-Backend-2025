using Microsoft.Extensions.Caching.Distributed;

using Newtonsoft.Json;

using Rinha.Api.Services;

namespace Rinha.Api.Clients;

public class PaymentGatewayClient(
    IHttpClientFactory httpClientFactory,
    CacheService cache,
    ILogger<PaymentGatewayClient> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly CacheService _cache = cache;
    private readonly ILogger<PaymentGatewayClient> _logger = logger;

    public async Task<HttpResponseMessage> PaymentAsync(
        Payment payment,
        PaymentGateway gateway = PaymentGateway.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());
        try
        {
            var result = await client.PostAsJsonAsync("payments", payment, cancellationToken);

            if (result.IsSuccessStatusCode)
            {
                await _cache.SetAsync(
                    $"payment:last:{gateway}",
                    payment,
                    cancellationToken: cancellationToken);
            }

            return result;
        }
        finally
        {
            client.Dispose();
        }

    }

    public async Task<Payment?> GetByCorrelationIdAsync(
        Guid correlationId,
        PaymentGateway gateway = PaymentGateway.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());

        try
        {
            var response = await client.GetAsync(
                $"payments/{correlationId}",
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Payment?>(
                cancellationToken: cancellationToken);

        }
        finally
        {
            client.Dispose();
        }
    }

    public async Task<HealthResponse> GetHealthAsync(
       PaymentGateway gateway = PaymentGateway.Default,
       CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());

        try
        {
            HealthResponse? health = await _cache.GetAsync<HealthResponse>(
                gateway.ToString(),
                cancellationToken: cancellationToken);

            if (health is not null)
            {
                return health;
            }

            health = await client.GetFromJsonAsync<HealthResponse>(
                "payments/service-health",
                cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("Failed to get health response from payment processor");

            await _cache.SetAsync(
                gateway.ToString(),
                JsonConvert.SerializeObject(health),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(5000)
                },
                cancellationToken);

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get health from '{gateway}' payment processor.", ex);
            return new HealthResponse(true, 0);
        }
        finally
        {
            client.Dispose();
        }
    }

}