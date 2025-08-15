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
            bool isOk = false;

            // Default
            if (_healthChecker.IsDefaultBest())
            {
                payment.Gateway = PaymentGateway.Default;
                var result = await _paymentClient.PaymentAsync(payment);
                isOk = result.IsSuccessStatusCode;
            }
            else
            {
                _healthChecker.SetUpdateHealthy(PaymentGateway.Default, false);
            }

            // Fallback
            if (_healthChecker.IsFallbackBest())
            {
                payment.Gateway = PaymentGateway.Default;
                var result = await _paymentClient.PaymentAsync(payment);
                isOk = result.IsSuccessStatusCode;
            }
            else
            {
                _healthChecker.SetUpdateHealthy(PaymentGateway.Fallback, false);
            }

            if (isOk)
            {
                await _processedQueue.EnqueueAsync(payment, cancellationToken);
                return;
            }

            throw new Exception("Both services unavailable.");
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