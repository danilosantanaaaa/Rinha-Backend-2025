using System.Net;

namespace Rinha.Api.Common;

public static class Configuration
{
    public const int TimeoutInMilliseconds = 1000;
    public const int MaxDegreeOfParallels = 30;
    public const int CacheLockedInSeconds = 5;

    public static Func<HttpMessageHandler> GetSocketHandler() => () => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = int.MaxValue,
    };
}