using Rinha.Api.Models.Payments;
using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class PaymentProcessorWorker : BackgroundService
{
    private readonly PaymentService _paymentService;
    private readonly MessageQueue<PaymentRequest> _requestQueue;
    private readonly ILogger<PaymentProcessorWorker> _logger;
    private readonly HealthChecker _healthChecker;

    public PaymentProcessorWorker(
        IServiceScopeFactory scopeFactory,
        MessageQueue<PaymentRequest> paymentQueue,
        ILogger<PaymentProcessorWorker> logger,
        HealthChecker healthChecker)
    {
        _paymentService = scopeFactory
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<PaymentService>();

        _requestQueue = paymentQueue;
        _logger = logger;
        _healthChecker = healthChecker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        // Start multiple payment processing tasks to handle payments concurrently
        for (var i = 0; i < Configuration.TasksInParallel; i++)
        {
            tasks.Add(ProcessPaymentAsync(i, stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessPaymentAsync(int workId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Worker Id {WorkerId} started processing payments", workId);
        while (await _requestQueue.WaitToReadAsync(stoppingToken))
        {
            try
            {
                var request = await _requestQueue.DequeueAsync(stoppingToken);
                await _paymentService.ProcessAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
            finally
            {
                // To ensure there is enough time to save to the database.
                if (_healthChecker.IsBothFailing())
                {
                    await Task.Delay(10, stoppingToken);
                }
                else
                {
                    await Task.Delay(30, stoppingToken);
                }
            }
        }
    }
}