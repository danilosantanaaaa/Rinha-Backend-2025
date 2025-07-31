using Rinha.Api.Repositories;

namespace Rinha.Api.Services;

public class PaymentService(
    PaymentRepository paymentRepository,
    ILogger<PaymentService> logger,
    HealthChecker circuitBreaker,
    MessageQueue<PaymentRequest> paymentQueue)
{
    private readonly PaymentRepository _paymentRepository = paymentRepository;
    private readonly ILogger<PaymentService> _logger = logger;
    private readonly HealthChecker _circuitBreaker = circuitBreaker;
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
            var isOk = await _circuitBreaker.ExecuteAsync(payment, cancellationToken);

            if (isOk)
            {
                await _paymentRepository.AddAsync(payment, _circuitBreaker.Gateway);
                return;
            }

            throw new Exception("Both services unavailable.");
        }
        catch (Exception ex)
        {
            await _paymentQueue.EnqueueAsync(request, cancellationToken);
            _logger.LogError($"Error: {ex.Message}", ex);
        }
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc)
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