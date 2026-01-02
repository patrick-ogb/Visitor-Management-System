using System.Threading.Channels;
using VMS.Core.Entities;

namespace VMS.Infrastructure.Services;

public class NotificationEventQueue
{
    private readonly Channel<NotificationEvent> _channel;

    public NotificationEventQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<NotificationEvent>(options);
    }

    public async Task EnqueueAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(notificationEvent, cancellationToken);
    }

    public async Task<NotificationEvent?> DequeueAsync(CancellationToken cancellationToken)
    {
        if (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_channel.Reader.TryRead(out var notificationEvent))
            {
                return notificationEvent;
            }
        }
        return null;
    }

    public IAsyncEnumerable<NotificationEvent> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

