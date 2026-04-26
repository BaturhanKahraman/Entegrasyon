using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Sse;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class NotificationReadSyncHandler(
    ISseConnectionRegistry registry) : IDomainEventHandler<NotificationReadEvent>
{
    public Task HandleAsync(NotificationReadEvent @event, CancellationToken ct = default)
    {
        var payload = new SseNotificationPayload
        {
            EventType = "notification.read",
            NotificationId = @event.NotificationId,
            ReadAt = @event.ReadAt
        };

        foreach (var ch in registry.GetChannels(@event.UserId))
        {
            ch.Writer.TryWrite(payload);
        }

        return Task.CompletedTask;
    }
}
