using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Abstract;

public interface INotificationManager
{
    Task SendNotification(
        string header,
        string content,
        NotificationSeverity severity,
        NotificationCategory category,
        IEnumerable<Guid> userIds,
        string? actionUrl = null);

    Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false);
    Task MarkAsRead(long notificationId, Guid userId);
    Task MarkAllAsRead(Guid userId);
}
