using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Chat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Blazor.Utility.Chat;

/// <summary>
/// EventChannel&lt;ChatMessageEvent&gt; kanalının TEK consumer'ı.
/// Hiçbir component bu kanaldan doğrudan okuma yapamaz.
/// Tüm Blazor bileşenleri IChatDeliveryService.Subscribe kullanır.
/// </summary>
public sealed class ChatEventPublisher(
    EventChannel<ChatMessageEvent> channel,
    IChatDeliveryService deliveryService,
    ILogger<ChatEventPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await deliveryService.DeliverAsync(evt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deliver chat message {MessageId}", evt.MessageId);
            }
        }
    }
}
