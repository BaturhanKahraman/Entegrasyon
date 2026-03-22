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
    IDbContextFactory<IntegrationDbContext> contextFactory,
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

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Kullanıcıların varlığını doğrula (track etmeden)
        var existingUserIds = await dbContext.Users
            .Where(u => userIdList.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        var notification = new Notification
        {
            Header = header,
            Content = content,
            Severity = severity,
            Category = category,
            ActionUrl = actionUrl,
            NotificationsUsers = existingUserIds.Select(uid => new NotificationsUsers
            {
                ApplicationUserId = uid
            }).ToList()
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        // Tüm sender'ları tetikle (email, signalr — implemente edildiğinde)
        if (existingUserIds.Count > 0)
        {
            await Task.WhenAll(notificationSenders.Select(s =>
                s.SendNotification(notification, existingUserIds)));
        }

        // EventChannel'a yaz → NotificationEventPublisher → INotificationDeliveryService
        var evt = new NotificationEvent(
            notification.Id, header, content, existingUserIds, severity, category, actionUrl);
        await eventChannel.Writer.WriteAsync(evt);
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false, int? take = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.Notifications
            .Where(n => n.Users.Any(u => u.Id == userId));

        if (onlyUnread)
            query = query.Where(n => !n.IsRead);

        query = query.OrderByDescending(n => n.CreatedAt);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync();
    }

    public async Task MarkAsRead(long notificationId, Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var notification = await dbContext.Notifications
            .Include(n => n.Users)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.Users.Any(u => u.Id == userId));

        if (notification is null) return;

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task MarkAllAsRead(Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await dbContext.Notifications
            .Where(n => n.Users.Any(u => u.Id == userId) && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));
    }
}
