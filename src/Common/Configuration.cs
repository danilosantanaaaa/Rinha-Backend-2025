using System.Net;

namespace Rinha.Api.Common;

public static class Configuration
{
    public const int MaxDegreeOfParallels = 20;

    public static Func<HttpMessageHandler> GetSocketHandler() => () => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = int.MaxValue,
    };
}