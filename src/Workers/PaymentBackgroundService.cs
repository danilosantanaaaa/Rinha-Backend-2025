using System.Threading.Channels;

using Rinha.Api.Models;
using Rinha.Api.Services;

namespace Rinha.Api.Workers;

public class PaymentBackgroundService : BackgroundService
{
    private readonly Channel<PaymentRequest> _channel;
    private readonly PaymentService _paymentService;
    private readonly ILogger<PaymentBackgroundService> _logger;

    public PaymentBackgroundService(
        Channel<PaymentRequest> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentBackgroundService> logger)
    {
        _channel = channel;
        _paymentService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<PaymentService>();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            try
            {
                var request = await _channel.Reader.ReadAsync();
                await _paymentService.ProcessAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment request");
            }
        }
    }
}