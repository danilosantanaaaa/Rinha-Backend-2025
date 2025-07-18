using Polly;
using Polly.Retry;

using Rinha.Api.Clients;
using Rinha.Api.Models;
using Rinha.Api.Repositories;

namespace Rinha.Api.Services;

public class PaymentService(
    PaymentRepository paymentRepository,
    PaymentProcessorClient paymentProcessorClient,
    ILogger<PaymentService> logger)
{
    private readonly PaymentRepository _paymentRepository = paymentRepository;
    private readonly PaymentProcessorClient _paymentProcessorClient = paymentProcessorClient;
    private readonly ILogger<PaymentService> _logger = logger;

    public async Task ProcessAsync(PaymentRequest request)
    {
        Payment payment = new Payment(
            request.CorrelationId,
            request.Amount,
            DateTime.Now);

        var predicateBuilder = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => !r.IsSuccessStatusCode);

        PaymentProcessorType type = PaymentProcessorType.Default;

        // Criando pipeline
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddFallback(new()
            {
                ShouldHandle = predicateBuilder,
                FallbackAction = async args =>
                {
                    _logger.LogInformation("Payment Fallback Processor");

                    // Try to resolve the fallback response
                    HttpResponseMessage fallbackResponse =
                        await _paymentProcessorClient.PaymentAsync(
                            payment,
                            PaymentProcessorType.Fallback);

                    type = PaymentProcessorType.Fallback;

                    return await Outcome.FromResultAsValueTask(fallbackResponse);
                }
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
            {
                MaxRetryAttempts = 3,

                OnRetry = args =>
                {
                    _logger.LogWarning("OnRetry, Attempt: {a}", args.AttemptNumber);

                    // Event handlers can be asynchronous; here, we return an empty ValueTask.
                    return default;
                }
            })
            .Build();

        // Executando o pipeline
        var result = await pipeline.ExecuteAsync(
            async token => await _paymentProcessorClient.PaymentAsync(
                payment,
                PaymentProcessorType.Default));

        if (!result.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Failed to process payment: {result.ReasonPhrase} in  Payment {type} Processor.");
        }

        await _paymentRepository.AddAsync(payment, type);
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTime from, DateTime to)
    {
        return await _paymentRepository.GetSummaryAsync(from, to);
    }
}