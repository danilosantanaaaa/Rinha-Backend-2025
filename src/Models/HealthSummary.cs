namespace Rinha.Api.Models;

public record HealthSummary
{
    public HealthResponse Default { get; private set; } = new HealthResponse(false, 0);
    public HealthResponse Fallback { get; private set; } = new HealthResponse(false, 0);
    public PaymentGateway BestServer { get; set; } = PaymentGateway.Default;

    public void SetDefault(
        HealthResponse @default)
    {
        Default = @default;
    }

    public void SetFallback(
        HealthResponse fallback)
    {
        Fallback = fallback;
    }

    public bool IsBothGatewaysUnhealthy()
    {
        return !Default.IsHealthy && !Fallback.IsHealthy;
    }

    public bool IsAnyGatewayHealthy() =>
        !IsBothGatewaysUnhealthy();


    public void SetBestGateway()
    {
        // Verificando se ambos os servidores estão com problemas
        if (IsBothGatewaysUnhealthy())
        {
            BestServer = PaymentGateway.Default;
            return;
        }

        if (Default.IsHealthy && Default.MinResponseTime <= 100)
        {
            BestServer = PaymentGateway.Default;
            return;
        }

        // Verificando se o servidor fallback é mais rápido que o default
        if (Default.IsHealthy && Fallback.IsHealthy && Default.MinResponseTime > Fallback.MinResponseTime)
        {
            BestServer = PaymentGateway.Fallback;
            return;
        }

        if (Default.IsHealthy)
        {
            BestServer = PaymentGateway.Default;
            return;
        }

        BestServer = PaymentGateway.Default;
    }
}