using Rinha.Api.Models;

namespace Rinha.Api.Clients;

public class PaymentProcessorClient(
    IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<HttpResponseMessage> PaymentAsync(
        Payment payment,
        PaymentProcessorType strategy = PaymentProcessorType.Default,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(strategy.ToString());
        var result = await client.PostAsJsonAsync("payments", payment, cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to process payment: {result.ReasonPhrase}");
        }

        return result;

    }

}