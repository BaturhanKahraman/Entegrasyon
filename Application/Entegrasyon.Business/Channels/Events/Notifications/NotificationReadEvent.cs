namespace Entegrasyon.Business.Channels.Events.Notifications;

public sealed class NotificationReadEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ReadAt { get; set; }

    public NotificationReadEvent() { }
    public NotificationReadEvent(long notificationId, Guid userId, DateTimeOffset readAt)
    {
        NotificationId = notificationId;
        UserId = userId;
        ReadAt = readAt;
    }
}
