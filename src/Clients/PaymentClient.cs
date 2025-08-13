using System.Net;

using RedLockNet;

using Rinha.Api.Services;

namespace Rinha.Api.Clients;

public sealed class PaymentClient(
    IHttpClientFactory httpClientFactory,
    CacheService cache,
    IDistributedLockFactory lockFactory,
    ILogger<PaymentClient> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly CacheService _cache = cache;
    private readonly IDistributedLockFactory _lockFactory = lockFactory;
    private readonly ILogger<PaymentClient> _logger = logger;

    public async Task<HttpResponseMessage> PaymentAsync(
        Payment payment,
        PaymentGateway gateway = PaymentGateway.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());

        try
        {
            var result = await client.PostAsJsonAsync("payments", payment, AppJsonSerializerContext.Default.Payment, cancellationToken);

            return result;
        }
        catch (Exception)
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            };
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
        client.Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutInMilliseconds);

        using var _lock = await _lockFactory.CreateLockAsync(
            nameof(PaymentClient),
            TimeSpan.FromSeconds(Configuration.CacheTimeExpiryInSeconds)
        );

        if (!_lock.IsAcquired)
        {
            return HealthResponse.Locked;
        }

        try
        {
            // Get for the cache
            HealthResponse? health = await _cache.GetAsync<HealthResponse>(gateway.ToString());

            if (health is not null)
            {
                return health;
            }

            var result = await client.GetAsync("/payments/service-health", cancellationToken);

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Occurred some error in {gateway} status code {result.StatusCode}");
            }

            health = await result.Content.ReadFromJsonAsync<HealthResponse>(
                    AppJsonSerializerContext.Default.HealthResponse,
                    cancellationToken)
                ?? throw new NullReferenceException("Ops. Don't can be deserializer.");

            await _cache.SetAsync(
                gateway.ToString(),
                health,
                TimeSpan.FromSeconds(Configuration.CacheTimeExpiryInSeconds));

            return health;
        }
        catch (Exception)
        {
            return HealthResponse.Error;
        }
        finally
        {
            client.Dispose();
            _lock.Dispose();
        }
    }

}