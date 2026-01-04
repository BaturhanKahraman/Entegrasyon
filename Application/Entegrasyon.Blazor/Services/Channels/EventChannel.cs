using System.Threading.Channels;

namespace Entegrasyon.Blazor.Services.Channels;

/// <summary>
/// Generic event channel for in-process event handling using .NET Channels.
/// Replaces RabbitMQ/Wolverine for single-instance scenarios.
/// </summary>
/// <typeparam name="TEvent">Type of event to handle</typeparam>
public class EventChannel<TEvent> where TEvent : class
{
    private readonly Channel<TEvent> _channel;

    public EventChannel(int capacity = 1000)
    {
        _channel = Channel.CreateBounded<TEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // Don't block producers
            SingleReader = false, // Multiple consumers
            SingleWriter = false  // Multiple producers
        });
    }

    /// <summary>
    /// Write an event to the channel
    /// </summary>
    public async ValueTask PublishAsync(TEvent evt, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(evt, cancellationToken);
    }

    /// <summary>
    /// Try to write an event synchronously
    /// </summary>
    public bool TryPublish(TEvent evt)
    {
        return _channel.Writer.TryWrite(evt);
    }

    /// <summary>
    /// Get reader for consuming events
    /// </summary>
    public ChannelReader<TEvent> Reader => _channel.Reader;

    /// <summary>
    /// Get writer for publishing events
    /// </summary>
    public ChannelWriter<TEvent> Writer => _channel.Writer;
}
