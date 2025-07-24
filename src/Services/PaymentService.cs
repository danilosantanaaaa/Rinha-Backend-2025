using System.Threading.Channels;

using Polly;
using Polly.Timeout;

using Rinha.Api.Clients;
using Rinha.Api.Helpers;
using Rinha.Api.Models;
using Rinha.Api.Repositories;

namespace Rinha.Api.Services;

public class PaymentService(
    PaymentRepository paymentRepository,
    PaymentProcessorClient paymentProcessorClient,
    ILogger<PaymentService> logger,
    HealthSummary health,
    Channel<PaymentRequest> channel)
{
    private readonly PaymentRepository _paymentRepository = paymentRepository;
    private readonly PaymentProcessorClient _paymentProcessorClient = paymentProcessorClient;
    private readonly ILogger<PaymentService> _logger = logger;
    private readonly HealthSummary _health = health;
    private readonly Channel<PaymentRequest> _channel = channel;

    public async Task ProcessAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        Payment payment = new Payment(
            request.CorrelationId,
            request.Amount,
            DateTime.Now);

        PaymentGateway bestServer = PaymentGateway.Default;

        while (true)
        {
            try
            {
                // Printar os healts abaixo com console default e fallback
                Console.WriteLine($"Health Default: {_health.Default.HealthResponse}");
                Console.WriteLine($"Health Fallback: {_health.Fallback.HealthResponse}");
                Console.WriteLine("-----------------------------------");

                _logger.LogInformation($"Attempt to process payment with strategy {bestServer}");

                if (IsBothGatewaysUnhealthy())
                {
                    _logger.LogWarning("Both payment processors are failing. Requeuing request.");
                    await _channel.Writer.WriteAsync(request, cancellationToken);
                    return;
                }

                bestServer = GetBestGateway();
                var result = await SendPaymentToGateway(payment, bestServer, cancellationToken);

                if (!result.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Payment request {bestServer} failed with status code {result.StatusCode}");

                }

                // Salvando no banco de dados
                await _paymentRepository.AddAsync(payment, bestServer);
                return;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment request after retries. Request requeued.");
            }
        }

    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTime from, DateTime to)
    {
        return await _paymentRepository.GetSummaryAsync(from, to);
    }

    private bool IsBothGatewaysUnhealthy()
    {
        return _health.Default.HealthResponse.Failing && _health.Fallback.HealthResponse.Failing;
    }

    private PaymentGateway GetBestGateway()
    {

        HealthResponse healthDefault = _health.Default.HealthResponse;
        HealthResponse healthFallback = _health.Fallback.HealthResponse;

        // Verificando se ambos os servidores estão com problemas
        if (!healthDefault.IsHealthy && !healthFallback.IsHealthy)
        {
            throw new InvalidOperationException("Both payment processors are failing.");
        }

        // Verificando se o servidor default é saudável e ainda é rapido o suficiente para mandar request
        if (healthDefault.IsHealthy && healthDefault.MinResponseTime <= 100)
        {
            return PaymentGateway.Default;
        }

        // Verificando se o servidor fallback é mais rápido que o default
        if (healthDefault.IsHealthy && healthFallback.IsHealthy && healthDefault.MinResponseTime > 1.7 * healthFallback.MinResponseTime)
        {
            return PaymentGateway.Fallback;
        }

        if (healthDefault.IsHealthy)
        {
            return PaymentGateway.Default;
        }

        return PaymentGateway.Fallback;
    }

    private async Task<HttpResponseMessage> SendPaymentToGateway(
        Payment payment,
        PaymentGateway gateway,
        CancellationToken cancellationToken)
    {
        // Criando pipeline
        AsyncTimeoutPolicy timeOut = Policy.TimeoutAsync(
            TimeSpan.FromMilliseconds(5000),
            TimeoutStrategy.Pessimistic,
            (context, timeout, _, exception) =>
            {
                Console.WriteLine($"{gateway}: {"{}:Timeout",-10}{timeout,-10:ss\\.fff}: {exception.GetType().Name}");
                return Task.CompletedTask;
            });

        return await timeOut.ExecuteAsync(async token =>
                await _paymentProcessorClient.PaymentAsync(
                    payment,
                    gateway),
                cancellationToken: cancellationToken);
    }

}