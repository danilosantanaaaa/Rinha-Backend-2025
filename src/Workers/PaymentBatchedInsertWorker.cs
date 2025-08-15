using Rinha.Api.Models.Payments;
using Rinha.Api.Repositories;

namespace Rinha.Api.Workers;

public sealed class PaymentBatchedInsertWorker(
    MessageQueue<Payment> processedQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentBatchedInsertWorker> logger) : BackgroundService
{
    private readonly MessageQueue<Payment> _processedQueue = processedQueue;
    private readonly ILogger<PaymentBatchedInsertWorker> _logger = logger;
    private readonly PaymentRepository _paymentRepository =
        scopeFactory.CreateScope().ServiceProvider.GetRequiredService<PaymentRepository>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _processedQueue.WaitToReadAsync(stoppingToken))
        {
            IEnumerable<Payment> payments = [];
            try
            {
                if (_processedQueue.Count >= Configuration.TasksInParallel)
                {
                    payments = await _processedQueue.ReadAllAsync(stoppingToken);
                    await _paymentRepository.AddRangeAsync(payments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                // Por precaução caso o Zan derrube o banco de dado, dar ruim se api cai com os dados.
                foreach (var payment in payments)
                {
                    await _processedQueue.EnqueueAsync(payment, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1), stoppingToken);
        }
    }
}