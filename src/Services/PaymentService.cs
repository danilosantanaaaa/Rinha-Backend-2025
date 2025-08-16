using Rinha.Api.Models.Payments;
using Rinha.Api.Models.Summaries;
using Rinha.Api.Repositories;

namespace Rinha.Api.Services;

public sealed class PaymentService
{
    private readonly PaymentRepository _paymentRepository;
    private readonly PaymentClient _paymentClient;
    private readonly ILogger<PaymentService> _logger;
    private readonly HealthChecker _healthChecker;
    private readonly MessageQueue<Payment> _processedQueue;
    private readonly MessageQueue<PaymentRequest> _requestQueue;

    public PaymentService(
        PaymentRepository paymentRepository,
        PaymentClient paymentClient,
        ILogger<PaymentService> logger,
        MessageQueue<Payment> processedQueue,
        MessageQueue<PaymentRequest> requestQueue,
        HealthChecker healthBreaker)
    {
        _paymentRepository = paymentRepository;
        _paymentClient = paymentClient;
        _logger = logger;
        _processedQueue = processedQueue;
        _requestQueue = requestQueue;
        _healthChecker = healthBreaker;
    }

    public async Task ProcessAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        Payment payment = new Payment(
            request.CorrelationId,
            request.Amount,
            DateTime.UtcNow);

        try
        {
            if (_healthChecker.IsBothFailing())
            {
                throw new Exception("Both services is unhealthy.");
            }

            payment.Gateway = _healthChecker.IsDefaultBest()
                ? PaymentGateway.Default
                : PaymentGateway.Fallback;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var result = await _paymentClient.PaymentAsync(payment, cancellationToken);
                if (result.IsSuccessStatusCode)
                {
                    await _processedQueue.EnqueueAsync(payment, cancellationToken);
                    return;
                }
                else
                {
                    _healthChecker.UpdateHealth(
                        gateway: payment.Gateway,
                        failing: true);
                }

                payment.Gateway = payment.Gateway == PaymentGateway.Default
                    ? PaymentGateway.Fallback
                    : PaymentGateway.Default;
            }

            throw new Exception("No attempt was successful in 3 attempt.");
        }
        catch (Exception ex)
        {
            await _requestQueue.EnqueueAsync(request, cancellationToken);
            _logger.LogError($"Error: {ex.Message}", ex);
        }
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        try
        {
            return await _paymentRepository.GetSummaryAsync(fromUtc, toUtc);
        }
        catch (Exception)
        {
            throw;
        }
    }
}