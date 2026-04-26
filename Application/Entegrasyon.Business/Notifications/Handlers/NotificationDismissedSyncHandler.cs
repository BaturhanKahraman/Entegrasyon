using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Sse;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class NotificationDismissedSyncHandler(
    ISseConnectionRegistry registry) : IDomainEventHandler<NotificationDismissedEvent>
{
    public Task HandleAsync(NotificationDismissedEvent @event, CancellationToken ct = default)
    {
        var payload = new SseNotificationPayload
        {
            EventType = "notification.dismissed",
            NotificationId = @event.NotificationId
        };

        foreach (var ch in registry.GetChannels(@event.UserId))
        {
            ch.Writer.TryWrite(payload);
        }

        return Task.CompletedTask;
    }
}
