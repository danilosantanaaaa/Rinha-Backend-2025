using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class PaymentBackgroundService : BackgroundService
{
    private readonly PaymentService _paymentService;
    private readonly MessageQueue<PaymentRequest> _paymentQueue;
    private readonly ILogger<PaymentBackgroundService> _logger;

    public PaymentBackgroundService(
        MessageQueue<PaymentRequest> paymentQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentBackgroundService> logger)
    {
        _paymentService = scopeFactory
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<PaymentService>();

        _paymentQueue = paymentQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        // Start multiple payment processing tasks to handle payments concurrently
        for (var i = 0; i < Configuration.WorkerParallel; i++)
        {
            tasks.Add(ProcessPaymentAsync(i, stoppingToken));
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
}