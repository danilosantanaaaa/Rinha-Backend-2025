using System.Threading.Channels;

using Rinha.Api.Helpers;
using Rinha.Api.Models;
using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class PaymentBackgroundService : BackgroundService
{
    private readonly PaymentService _paymentService;
    private readonly Channel<PaymentRequest> _channel;
    private readonly ILogger<PaymentBackgroundService> _logger;

    public PaymentBackgroundService(
        Channel<PaymentRequest> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentBackgroundService> logger)
    {
        _paymentService = scopeFactory
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<PaymentService>();

        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        for (var i = 0; i < 20; i++)
        {
            tasks.Add(ProcessPaymentAsync(stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessPaymentAsync(CancellationToken stoppingToken)
    {
        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            try
            {
                var request = await _channel.Reader.ReadAsync(stoppingToken);
                await _paymentService.ProcessAsync(request, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment request");
            }
        }
    }

}