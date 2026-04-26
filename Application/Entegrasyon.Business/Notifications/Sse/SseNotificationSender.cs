using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Sse;

public sealed class SseNotificationSender(ISseConnectionRegistry registry) : INotificationSender
{
    public SenderType Type => SenderType.Sse;

    public Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            var payload = new SseNotificationPayload
            {
                EventType = "notification",
                NotificationId = message.Id,
                Header = message.Header,
                Content = message.Content,
                Severity = message.Severity.ToString(),
                Category = message.Category.ToString(),
                ActionUrl = message.ActionUrl,
                CreatedAt = message.CreatedAt
            };
            foreach (var ch in registry.GetChannels(userId))
            {
                ch.Writer.TryWrite(payload);  // non-blocking; if channel full, DropOldest semantics from BoundedChannelOptions
            }
        }
        return Task.CompletedTask;
    }
}
