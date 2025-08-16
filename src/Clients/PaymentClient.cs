using System.Net;

using Rinha.Api.Models.Healths;
using Rinha.Api.Models.Payments;
using Rinha.Api.Repositories;

namespace Rinha.Api.Clients;

public sealed class PaymentClient(
    IHttpClientFactory httpClientFactory,
    PaymentCacheRepository cache,
    IDistributedLockFactory lockFactory,
    ILogger<PaymentClient> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly PaymentCacheRepository _cache = cache;
    private readonly IDistributedLockFactory _lockFactory = lockFactory;
    private readonly ILogger<PaymentClient> _logger = logger;

    public async Task<HttpResponseMessage> PaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(payment.Gateway.ToString());

        try
        {
            var result = await client.PostAsJsonAsync(
                "/payments",
                payment,
                AppJsonSerializerContext.Default.Payment,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);

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

    public async Task<HealthResponse?> GetHealthAsync(
        PaymentGateway gateway = PaymentGateway.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());

        using var _lock = await _lockFactory.CreateLockAsync(
            resource: $"payment_{gateway}",
            expiryTime: TimeSpan.FromMicroseconds(5010));

        try
        {
            if (!_lock.IsAcquired)
            {
                _logger.LogInformation($"Resource {gateway} lock!");
                return null;
            }

            // Get from the cache
            HealthResponse? health = await _cache.GetHealthAsync(gateway.ToString());

            if (health is not null)
            {
                return health;
            }

            var result = await client.GetAsync("/payments/service-health", cancellationToken);

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Occurred some error in {gateway} with status code {result.StatusCode}");
            }

            health = await result.Content.ReadFromJsonAsync<HealthResponse>(
                    AppJsonSerializerContext.Default.HealthResponse,
                    cancellationToken)
                ?? throw new NullReferenceException("Ops. Don't can be deserializer.");

            await _cache.SetHealthAsync(
                gateway.ToString(),
                health,
                TimeSpan.FromMilliseconds(5010));

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
        finally
        {
            client.Dispose();
        }

        return default!;
    }

}