namespace Entegrasyon.Business.Channels.Events.Notifications;

public sealed class NotificationDismissedEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public Guid UserId { get; set; }

    public NotificationDismissedEvent() { }
    public NotificationDismissedEvent(long notificationId, Guid userId)
    {
        NotificationId = notificationId;
        UserId = userId;
    }
}
