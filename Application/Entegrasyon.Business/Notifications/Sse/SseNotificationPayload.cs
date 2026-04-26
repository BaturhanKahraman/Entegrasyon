namespace Entegrasyon.Business.Notifications.Sse;

public sealed class SseNotificationPayload
{
    public string EventType { get; set; } = "notification";
    public long? NotificationId { get; set; }
    public string? Header { get; set; }
    public string? Content { get; set; }
    public string? Severity { get; set; }
    public string? Category { get; set; }
    public string? ActionUrl { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
