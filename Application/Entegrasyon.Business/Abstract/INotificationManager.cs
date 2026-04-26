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

    Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false, int? take = null);

    /// <summary>
    /// Bildirimler sayfası için: junction eager load + tab/category/severity filtreleri SQL'de uygulanır.
    /// </summary>
    Task<List<Notification>> GetNotificationsPageAsync(
        Guid userId,
        string tab = "all",
        NotificationCategory? category = null,
        NotificationSeverity? severity = null);

    Task MarkAsRead(long notificationId, Guid userId);
    Task MarkAllAsRead(Guid userId);
    Task DismissNotification(long notificationId, Guid userId);
    Task DismissAllRead(Guid userId);
    Task<List<Notification>> GetAllNotificationsAsync(int take = 200);
}
