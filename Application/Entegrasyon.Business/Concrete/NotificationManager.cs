using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class NotificationManager(
    IEnumerable<INotificationSender> notificationSenders,
    IntegrationDbContext context,
    IFluentValidator validator,
    EventChannel<NotificationEvent> eventChannel) : INotificationManager
{
    public async Task SendNotification(
        string header,
        string content,
        NotificationSeverity severity,
        NotificationCategory category,
        IEnumerable<Guid> userIds,
        string? actionUrl = null)
    {
        // 1. Validation
        var userIdList = userIds.ToList();
        var request = new SendNotificationRequest(header, content, severity, category, userIdList, actionUrl);
        await validator.ValidateAndThrowAsync(request);

        // 2. Business Rules — (genişleme noktası)

        // 3. Execution

        var trackedUsers = await context.Users
            .Where(u => userIdList.Contains(u.Id))
            .ToListAsync();

        var notification = new Notification
        {
            Header = header,
            Content = content,
            Severity = severity,
            Category = category,
            ActionUrl = actionUrl,
            Users = trackedUsers
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Tüm sender'ları tetikle (email, signalr — implemente edildiğinde)
        var trackedUserIds = trackedUsers.Select(u => u.Id).ToList();
        if (trackedUsers.Count > 0)
        {
            await Task.WhenAll(notificationSenders.Select(s =>
                s.SendNotification(notification, trackedUserIds)));
        }

        // EventChannel'a yaz → NotificationEventPublisher → INotificationDeliveryService
        // Sadece DB'de var olan kullanıcılara event gönder
        var evt = new NotificationEvent(
            notification.Id, header, content, trackedUserIds, severity, category, actionUrl);
        await eventChannel.Writer.WriteAsync(evt);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false)
    {
        var query = context.Notifications
            .Where(n => n.Users.Any(u => u.Id == userId));

        if (onlyUnread)
            query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task MarkAsRead(long notificationId, Guid userId)
    {
        var notification = await context.Notifications
            .Include(n => n.Users)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.Users.Any(u => u.Id == userId));

        if (notification is null) return;

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task MarkAllAsRead(Guid userId)
    {
        await context.Notifications
            .Where(n => n.Users.Any(u => u.Id == userId) && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));
    }
}
