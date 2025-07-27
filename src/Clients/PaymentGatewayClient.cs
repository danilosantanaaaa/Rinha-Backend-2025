using Microsoft.Extensions.Caching.Distributed;

using Newtonsoft.Json;

using Rinha.Api.Services;

namespace Rinha.Api.Clients;

public class PaymentGatewayClient(
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache,
    ILogger<PaymentGatewayClient> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<PaymentGatewayClient> _logger = logger;

    public async Task<HttpResponseMessage> PaymentAsync(
        Payment payment,
        PaymentGateway gateway = PaymentGateway.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());
        client.Timeout = TimeSpan.FromMilliseconds(1000);
        try
        {
            var result = await client.PostAsJsonAsync("payments", payment, cancellationToken);

            if (!result.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to process payment: {result.ReasonPhrase}");
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
            var healthCached = await _cache.GetStringAsync(
                gateway.ToString(),
                token: cancellationToken);

            HealthResponse health;

            if (!string.IsNullOrEmpty(healthCached))
            {
                return JsonConvert.DeserializeObject<HealthResponse>(healthCached)!;
            }

            health = await client.GetFromJsonAsync<HealthResponse>(
                "payments/service-health",
                cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("Failed to get health response from payment processor");

            await _cache.SetStringAsync(
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