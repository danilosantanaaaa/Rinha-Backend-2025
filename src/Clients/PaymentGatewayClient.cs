using System.Net;

using Microsoft.Extensions.Caching.Distributed;

using Rinha.Api.Services;

namespace Rinha.Api.Clients;

public sealed class PaymentGatewayClient(
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

    public async Task<HttpResponseMessage> GetHealthAsync(
        Payment payment,
        PaymentGateway gateway = PaymentGateway.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(gateway.ToString());
        client.Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutInMilliseconds);

        try
        {
            var result = await client.PostAsJsonAsync("payments", payment, cancellationToken);

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

}