using Rinha.Api.Repositories;

namespace Rinha.Api.Services;

public sealed class PaymentService(
    PaymentRepository paymentRepository,
    PaymentGatewayClient paymentClient,
    ILogger<PaymentService> logger,
    HealthChecker circuitBreaker,
    MessageQueue<PaymentRequest> paymentQueue)
{
    private readonly PaymentRepository _paymentRepository = paymentRepository;
    private readonly PaymentGatewayClient _paymentClient = paymentClient;
    private readonly ILogger<PaymentService> _logger = logger;
    private readonly HealthChecker _healthChecker = circuitBreaker;
    private readonly MessageQueue<PaymentRequest> _paymentQueue = paymentQueue;

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
            // Default
            if (_healthChecker.IsDefaultHealth)
            {
                var result = await _paymentClient.PaymentAsync(payment, PaymentGateway.Default);

                if (result.IsSuccessStatusCode)
                {
                    await _paymentRepository.AddAsync(payment, PaymentGateway.Default);
                    return;
                }
            }

            _healthChecker.SetUpdateHealth(PaymentGateway.Default, false, cancellationToken);

            // Fallback
            if (_healthChecker.IsFallbackHealth)
            {
                var result = await _paymentClient.PaymentAsync(payment, PaymentGateway.Fallback);

                if (result.IsSuccessStatusCode)
                {
                    await _paymentRepository.AddAsync(payment, PaymentGateway.Fallback);
                    return;
                }
            }

            _healthChecker.SetUpdateHealth(PaymentGateway.Fallback, false, cancellationToken);

            throw new Exception("Both services unavailable.");
        }
        catch (Exception ex)
        {
            await _paymentQueue.EnqueueAsync(request, cancellationToken);
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