using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications;

public record SendNotificationRequest(
    string Header,
    string Content,
    NotificationSeverity Severity,
    NotificationCategory Category,
    IEnumerable<Guid> UserIds,
    string? ActionUrl);
