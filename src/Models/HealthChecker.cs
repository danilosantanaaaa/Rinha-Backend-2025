using System.Net;

namespace Rinha.Api.Models;

public class HealthChecker(PaymentGatewayClient _client)
{
    private readonly PaymentGatewayClient _client = _client;

    public CircuitBreakerState State { get; set; }
    public SemaphoreSlim _lockClose = new SemaphoreSlim(1);
    public SemaphoreSlim _lockHalfOpen = new SemaphoreSlim(1);
    public SemaphoreSlim _lockOpen = new SemaphoreSlim(1);
    public PaymentGateway Gateway { get; private set; } = PaymentGateway.Default;
    public bool DefaultOnline { get; private set; } = true;
    public bool FallbackOnline { get; private set; } = true;

    public async Task<HttpResponseMessage> CloseAsync(Payment payment)
    {
        try
        {
            await _lockClose.WaitAsync();
            var result = await _client.PaymentAsync(payment, PaymentGateway.Default);

            if (result.IsSuccessStatusCode)
            {
                Gateway = PaymentGateway.Default;
                State = CircuitBreakerState.Close;

                return result;
            }
            else
            {
                State = CircuitBreakerState.HalfOpen;
            }

            return result;
        }
        finally
        {
            _lockClose.Release();
        }
    }

    public async Task<HttpResponseMessage> HalfOpenAsync(Payment payment)
    {
        try
        {
            await _lockHalfOpen.WaitAsync();
            var result = await _client.PaymentAsync(payment, PaymentGateway.Fallback);

            if (result.IsSuccessStatusCode)
            {
                Gateway = PaymentGateway.Fallback;
                State = CircuitBreakerState.Close;

                return result;
            }
            else
            {
                State = CircuitBreakerState.Close;
            }

            return result;
        }
        finally
        {
            _lockHalfOpen.Release();
        }
    }

    public async Task<HttpResponseMessage> OpenAsync()
    {
        try
        {
            await _lockOpen.WaitAsync();

            State = CircuitBreakerState.Open;
            Gateway = PaymentGateway.Default;
            return await Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable
            });
        }
        finally
        {
            _lockOpen.Release();
        }
    }

    public async Task<bool> ExecuteAsync(Payment payment, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        if (State == CircuitBreakerState.Close)
        {
            response = await CloseAsync(payment);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }

        if (State == CircuitBreakerState.HalfOpen)
        {
            response = await HalfOpenAsync(payment);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }

        await OpenAsync();

        return false;
    }

    public void SetDefault(
        bool defaultOnline)
    {
        DefaultOnline = defaultOnline;
    }

    public void SetFallback(
        bool fallbackOnline)
    {
        FallbackOnline = fallbackOnline;
    }
}