using Polly;
using Polly.Timeout;

using Rinha.Api.Repositories;

namespace Rinha.Api.Services;

public class PaymentService(
    PaymentRepository paymentRepository,
    PaymentGatewayClient paymentProcessorClient,
    ILogger<PaymentService> logger,
    HealthSummary health,
    [FromKeyedServices(Constaint.PaymentRetry)] MessageQueue<PaymentRequest> paymentRetryQueue)
{
    private readonly PaymentRepository _paymentRepository = paymentRepository;
    private readonly PaymentGatewayClient _paymentProcessorClient = paymentProcessorClient;
    private readonly ILogger<PaymentService> _logger = logger;
    private readonly HealthSummary _health = health;
    private readonly MessageQueue<PaymentRequest> _paymentRetryQueue = paymentRetryQueue;

    public async Task ProcessAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        Payment payment = new Payment(
            request.CorrelationId,
            request.Amount,
            DateTime.Now);

        PaymentGateway bestServer = GetBestGateway();
        HttpResponseMessage? response = null;
        try
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (_health.IsBothGatewaysUnhealthy())
                {
                    throw new InvalidOperationException("Both payment processors are failing.");
                }

                response = await SendPaymentToGateway(
                    payment,
                    bestServer,
                    cancellationToken);

                if (response?.IsSuccessStatusCode ?? false)
                {
                    _logger.LogInformation($"Payment {request.CorrelationId} processed successfully with strategy {bestServer}");
                    await _paymentRepository.AddAsync(payment, bestServer);
                    return;
                }

                bestServer = bestServer == PaymentGateway.Default
                    ? PaymentGateway.Fallback
                    : PaymentGateway.Default;
            }

            if (response?.IsSuccessStatusCode ?? false)
            {
                throw new InvalidOperationException($"Payment {request.CorrelationId} failed after 3 attempts with strategy {bestServer}");
            }

        }
        catch (Exception ex)
        {
            await _paymentRetryQueue.EnqueueAsync(request, cancellationToken);
            _logger.LogError(ex.Message, ex);
        }
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to)
    {
        return await _paymentRepository.GetSummaryAsync(from.UtcDateTime, to.UtcDateTime);
    }

    private PaymentGateway GetBestGateway()
    {
        HealthResponse def = _health.Default;
        HealthResponse fal = _health.Fallback;

        // Verificando se ambos os servidores estão com problemas
        if (!def.IsHealthy && !fal.IsHealthy)
        {
            throw new InvalidOperationException("Both payment processors are failing.");
        }

        if (!def.IsHealthy && def.MinResponseTime <= 100)
        {
            return PaymentGateway.Default;
        }

        // Verificando se o servidor fallback é mais rápido que o default
        if (def.IsHealthy && fal.IsHealthy && def.MinResponseTime > 1.7 * fal.MinResponseTime)
        {
            return PaymentGateway.Fallback;
        }

        if (def.IsHealthy)
        {
            return PaymentGateway.Default;
        }

        return PaymentGateway.Fallback;
    }

    private async Task<HttpResponseMessage?> SendPaymentToGateway(
        Payment payment,
        PaymentGateway gateway,
        CancellationToken cancellationToken)
    {

        HttpResponseMessage? response = null;

        // Criando pipeline
        AsyncTimeoutPolicy timeOut = Policy.TimeoutAsync(
            TimeSpan.FromMilliseconds(5000),
            TimeoutStrategy.Pessimistic,
            (context, timeout, _, exception) =>
            {
                _logger.LogInformation($"{gateway}: {"{}:Timeout",-10}{timeout,-10:ss\\.fff}: {exception.GetType().Name}");
                return Task.CompletedTask;
            });

        try
        {
            response = await timeOut.ExecuteAsync(async token =>
                await _paymentProcessorClient.PaymentAsync(
                    payment,
                    gateway),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }

        return response;
    }

}