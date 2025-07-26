using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class PaymentBackgroundService : BackgroundService
{
    private readonly PaymentService _paymentService;
    private readonly MessageQueue<PaymentRequest> _paymentQueue;
    private readonly MessageQueue<PaymentRequest> _paymentRetryQueue;
    private readonly ILogger<PaymentBackgroundService> _logger;
    private readonly HealthSummary _health;

    public PaymentBackgroundService(
        [FromKeyedServices(Configuration.PaymentQueue)] MessageQueue<PaymentRequest> paymentQueue,
        [FromKeyedServices(Configuration.PaymentRetry)] MessageQueue<PaymentRequest> paymentRetryQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentBackgroundService> logger,
        HealthSummary health)
    {
        _paymentService = scopeFactory
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<PaymentService>();

        _paymentQueue = paymentQueue;
        _paymentRetryQueue = paymentRetryQueue;
        _logger = logger;
        _health = health;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        // Start multiple payment processing tasks to handle payments concurrently
        for (var i = 0; i < 20; i++)
        {
            tasks.Add(ProcessPaymentAsync(i, stoppingToken));
        }

        // Start multiple retry tasks to handle retries concurrently
        for (var i = 20; i < 30; i++)
        {
            tasks.Add(RetryPaymentAsync(i, stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessPaymentAsync(int workId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Worker Id {WorkerId} started processing payments", workId);
        while (await _paymentQueue.WaitToReadAsync(stoppingToken))
        {
            try
            {

                var request = await _paymentQueue.DequeueAsync(stoppingToken);
                await _paymentService.ProcessAsync(request, stoppingToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }
    }

    private async Task RetryPaymentAsync(int workId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retry Worker Id {WorkerId} started processing payments", workId);
        while (await _paymentRetryQueue.WaitToReadAsync(stoppingToken))
        {
            try
            {
                var request = await _paymentRetryQueue.DequeueAsync(stoppingToken);
                await _paymentService.ProcessAsync(request, stoppingToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }
    }

}