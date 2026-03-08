using System.Threading.Channels;

namespace Entegrasyon.Business.Channels;

public class EventChannel<TEvent> where TEvent : class
{
    private readonly Channel<TEvent> _channel;

    public EventChannel(int capacity = 1000)
    {
        _channel = Channel.CreateBounded<TEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async ValueTask PublishAsync(TEvent evt, CancellationToken cancellationToken = default)
        => await _channel.Writer.WriteAsync(evt, cancellationToken);

    public bool TryPublish(TEvent evt)
        => _channel.Writer.TryWrite(evt);

    public ChannelReader<TEvent> Reader => _channel.Reader;
    public ChannelWriter<TEvent> Writer => _channel.Writer;
}
