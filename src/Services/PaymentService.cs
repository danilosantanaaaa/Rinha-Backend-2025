using Polly;
using Polly.Timeout;

using Rinha.Api.Repositories;

namespace Rinha.Api.Services;

public class PaymentService(
    PaymentRepository paymentRepository,
    PaymentGatewayClient gatewayClient,
    ILogger<PaymentService> logger,
    HealthSummary health,
    [FromKeyedServices(Configuration.PaymentRetry)] MessageQueue<PaymentRequest> paymentRetryQueue)
{
    private readonly PaymentRepository _paymentRepository = paymentRepository;
    private readonly PaymentGatewayClient _gatewayClient = gatewayClient;
    private readonly ILogger<PaymentService> _logger = logger;
    private readonly HealthSummary _health = health;
    private readonly MessageQueue<PaymentRequest> _paymentRetryQueue = paymentRetryQueue;

    public static PaymentGateway Gateway = PaymentGateway.Default;

    public async Task ProcessAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        Payment payment = new Payment(
            request.CorrelationId,
            request.Amount,
            DateTime.UtcNow);

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
                    Gateway,
                    cancellationToken);

                if (response?.IsSuccessStatusCode ?? false)
                {
                    _logger.LogInformation($"Payment {request.CorrelationId} processed successfully with strategy {Gateway}");
                    await _paymentRepository.AddAsync(payment, Gateway);
                    return;
                }
            }

            if (response?.IsSuccessStatusCode ?? true)
            {
                throw new InvalidOperationException($"Payment {request.CorrelationId} failed after 3 attempts with strategy {Gateway}");
            }

        }
        catch (Exception ex)
        {
            Gateway = Gateway == PaymentGateway.Default
                ? PaymentGateway.Fallback
                : PaymentGateway.Default;

            await _paymentRetryQueue.EnqueueAsync(request, cancellationToken);
            _logger.LogError(ex.Message, ex);
        }
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc)
    {
        return await _paymentRepository.GetSummaryAsync(fromUtc, toUtc);
    }

    private async Task<HttpResponseMessage?> SendPaymentToGateway(
        Payment payment,
        PaymentGateway gateway,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        // Criando pipeline
        try
        {
            response = await _gatewayClient.PaymentAsync(
                    payment,
                    gateway,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }

        return response;
    }

}