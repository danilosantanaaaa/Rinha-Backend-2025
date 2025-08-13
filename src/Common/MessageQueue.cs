using System.Threading.Channels;

namespace Rinha.Api.Common;

public sealed class MessageQueue<TModel> where TModel : class
{
    public readonly Channel<TModel> Queue;

    public MessageQueue()
    {
        Queue = Channel.CreateUnbounded<TModel>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public async Task EnqueueAsync(TModel request, CancellationToken cancellationToken = default)
    {
        await Queue.Writer.WriteAsync(request, cancellationToken);
    }

    public async Task<TModel> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await Queue.Reader.ReadAsync(cancellationToken);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return await Queue.Reader.WaitToReadAsync(cancellationToken);
    }
}