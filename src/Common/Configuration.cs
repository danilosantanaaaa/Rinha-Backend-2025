namespace Rinha.Api.Common;

public static class Configuration
{
    public const string PaymentQueue = "PaymentQueue";
    public const string PaymentRetry = "PaymentRetryQueue";
    public const int TimeoutInMiliSeconds = 110;
}