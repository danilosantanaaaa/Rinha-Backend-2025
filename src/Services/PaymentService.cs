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
                _logger.LogInformation($"Attempt to process payment with strategy {bestServer}");

                var result = await SendPaymentToGateway(payment, PaymentGateway.Default, cancellationToken);

                if (IsBothGatewaysUnhealthy())
                {
                    _logger.LogWarning("Both payment processors are failing. Requeuing request.");
                    await _channel.Writer.WriteAsync(request, cancellationToken);
                    break;
                }

                bestServer = GetBestGateway();

                if (!result.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Payment request {bestServer} failed with status code {result.StatusCode}");
                }

                // Salvando no banco de dados
                await _paymentRepository.AddAsync(payment, bestServer);
                break;

            }
            catch (Exception ex)
            {
                // bestServer = bestServer == PaymentGateway.Default
                //     ? PaymentGateway.Fallback
                //     : PaymentGateway.Default;

                _logger.LogError(ex, "Failed to process payment request after retries. Request requeued.");
                Console.WriteLine($"Failed to process payment request: {request} {bestServer.ToString()}");
            }
        }

    }

    private PaymentGateway GetBestGateway()
    {

        HealthResponse healthDefault = _health.Default.HealthResponse;
        HealthResponse healthFallback = _health.Fallback.HealthResponse;

        // Verificando se ambos os servidores estão com problemas
        if (healthDefault.Failing && healthFallback.Failing)
        {
            throw new InvalidOperationException("Both payment processors are failing.");
        }

        // Verificando se o servidor fallback é mais rápido que o default
        if (!healthDefault.Failing && !healthFallback.Failing && healthDefault.MinResponseTime >= 1.7 * healthFallback.MinResponseTime)
        {
            return PaymentGateway.Fallback;
        }

        // Se o servidor default não estiver falhando, retornamos ele
        if (!healthDefault.Failing)
        {
            return PaymentGateway.Default;
        }

        return PaymentGateway.Default;
    }

    private async Task<HttpResponseMessage> SendPaymentToGateway(
        Payment payment,
        PaymentGateway type,
        CancellationToken cancellationToken)
    {
        AsyncTimeoutPolicy timeOut = TimeoutFactory(6000);

        return await timeOut.ExecuteAsync(async token =>
                await _paymentProcessorClient.PaymentAsync(
                    payment,
                    type),
                cancellationToken: cancellationToken);
    }

    private static AsyncTimeoutPolicy TimeoutFactory(int milliseconds = 30_000)
    {
        // Criando pipeline
        return Policy.TimeoutAsync(
            TimeSpan.FromMilliseconds(milliseconds),
            TimeoutStrategy.Pessimistic,
            (context, timeout, _, exception) =>
            {
                Console.WriteLine($"{"Timeout",-10}{timeout,-10:ss\\.fff}: {exception.GetType().Name}");
                return Task.CompletedTask;
            });
    }

    public async Task<SummaryResponse> GetSummaryAsync(DateTime from, DateTime to)
    {
        return await _paymentRepository.GetSummaryAsync(from, to);
    }

    private bool IsBothGatewaysUnhealthy()
    {
        return _health.Default.HealthResponse.Failing && _health.Fallback.HealthResponse.Failing;
    }
}