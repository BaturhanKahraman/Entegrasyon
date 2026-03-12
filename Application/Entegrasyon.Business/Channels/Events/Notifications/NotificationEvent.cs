using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Channels.Events.Notifications;

public class NotificationEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public string Header { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public IEnumerable<Guid> UserIds { get; set; } = [];
    public NotificationSeverity Severity { get; set; }
    public NotificationCategory Category { get; set; }
    public string? ActionUrl { get; set; }

    public NotificationEvent() { }

    public NotificationEvent(long notificationId, string header, string content,
        IEnumerable<Guid> userIds, NotificationSeverity severity,
        NotificationCategory category, string? actionUrl = null)
    {
        NotificationId = notificationId;
        Header = header;
        Content = content;
        UserIds = userIds;
        Severity = severity;
        Category = category;
        ActionUrl = actionUrl;
    }
}
