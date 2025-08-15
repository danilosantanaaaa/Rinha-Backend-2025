using System.Net;

namespace Rinha.Api.Common;

public static class Configuration
{
    public const int TimeoutInMilliseconds = 1000;
    public const int TasksInParallel = 30;
    public const int CacheLockedInSeconds = 5;

    public static Func<HttpMessageHandler> GetSocketHandler() => () => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = int.MaxValue,
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
        EnableMultipleHttp2Connections = true,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };
}