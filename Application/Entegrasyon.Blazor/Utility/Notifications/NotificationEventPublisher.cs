using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Blazor.Utility.Notifications;

/// <summary>
/// EventChannel&lt;NotificationEvent&gt; kanalının TEK consumer'ı.
/// Hiçbir component bu kanaldan doğrudan okuma yapamaz.
/// Tüm Blazor bileşenleri INotificationDeliveryService.Subscribe kullanır.
/// </summary>
public sealed class NotificationEventPublisher(
    EventChannel<NotificationEvent> channel,
    INotificationDeliveryService deliveryService,
    ILogger<NotificationEventPublisher> logger) : BackgroundService
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
                logger.LogError(ex, "Failed to deliver notification {NotificationId}", evt.NotificationId);
            }
        }
    }
}
