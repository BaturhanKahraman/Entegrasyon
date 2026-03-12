namespace Entegrasyon.Business.Channels.Events.Notifications;

public class NotificationEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public string Header { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public IEnumerable<Guid> UserIds { get; set; } = [];

    public NotificationEvent() { }
    public NotificationEvent(long notificationId, string header, string content, IEnumerable<Guid> userIds)
    {
        NotificationId = notificationId;
        Header = header;
        Content = content;
        UserIds = userIds;
    }
}
