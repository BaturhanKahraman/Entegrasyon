using System.Threading.Channels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class NotificationManager(
    IEnumerable<INotificationSender> notificationSenders,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    Channel<BaseEvent> ephemeralChannel,
    ITenantContext tenantContext,
    ILogger<NotificationManager> logger) : INotificationManager
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

        // Tüm sender'ları tetikle — her biri izole, birinin hatası diğerini etkilemez
        foreach (var sender in notificationSenders)
        {
            try
            {
                await sender.SendNotification(notification, existingUserIds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bildirim gönderimi başarısız: {SenderType}, NotificationId: {NotificationId}",
                    sender.Type, notification.Id);
            }
        }
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false, int? take = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.Notifications
            .Where(n => n.NotificationsUsers.Any(nu => nu.ApplicationUserId == userId && !nu.IsDismissed));

        if (onlyUnread)
            query = query.Where(n => n.NotificationsUsers.Any(
                nu => nu.ApplicationUserId == userId && !nu.IsRead));

        query = query.OrderByDescending(n => n.CreatedAt);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync();
    }

    public async Task MarkAsRead(long notificationId, Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var affected = await dbContext.Set<NotificationsUsers>()
            .Where(nu => nu.NotificationId == notificationId
                      && nu.ApplicationUserId == userId
                      && !nu.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(nu => nu.IsRead, true)
                .SetProperty(nu => nu.ReadAt, DateTimeOffset.UtcNow));

        if (affected > 0)
        {
            await ephemeralChannel.Writer.WriteAsync(
                new NotificationReadEvent(notificationId, userId, DateTimeOffset.UtcNow)
                {
                    TenantId = tenantContext.TenantId
                });
        }
    }

    public async Task MarkAllAsRead(Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await dbContext.Set<NotificationsUsers>()
            .Where(nu => nu.ApplicationUserId == userId && !nu.IsDismissed && !nu.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(nu => nu.IsRead, true)
                .SetProperty(nu => nu.ReadAt, DateTimeOffset.UtcNow));
    }

    public async Task DismissNotification(long notificationId, Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var affected = await dbContext.Set<NotificationsUsers>()
            .Where(nu => nu.NotificationId == notificationId && nu.ApplicationUserId == userId && !nu.IsDismissed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(nu => nu.IsDismissed, true)
                .SetProperty(nu => nu.DismissedAt, DateTimeOffset.UtcNow));

        if (affected > 0)
        {
            await ephemeralChannel.Writer.WriteAsync(
                new NotificationDismissedEvent(notificationId, userId)
                {
                    TenantId = tenantContext.TenantId
                });
        }
    }

    public async Task DismissAllRead(Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await dbContext.Set<NotificationsUsers>()
            .Where(nu => nu.ApplicationUserId == userId && nu.IsRead && !nu.IsDismissed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(nu => nu.IsDismissed, true)
                .SetProperty(nu => nu.DismissedAt, DateTimeOffset.UtcNow));
    }

    public async Task<List<Notification>> GetNotificationsPageAsync(
        Guid userId,
        string tab = "all",
        NotificationCategory? category = null,
        NotificationSeverity? severity = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.Notifications
            .Include(n => n.NotificationsUsers.Where(nu => nu.ApplicationUserId == userId))
            .Where(n => n.NotificationsUsers.Any(nu => nu.ApplicationUserId == userId && !nu.IsDismissed));

        query = tab switch
        {
            "unread" => query.Where(n => n.NotificationsUsers.Any(nu => nu.ApplicationUserId == userId && !nu.IsRead)),
            "read"   => query.Where(n => n.NotificationsUsers.Any(nu => nu.ApplicationUserId == userId && nu.IsRead)),
            _        => query
        };

        if (category.HasValue)
            query = query.Where(n => n.Category == category.Value);

        if (severity.HasValue)
            query = query.Where(n => n.Severity == severity.Value);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetAllNotificationsAsync(int take = 200)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.Notifications
            .Include(n => n.NotificationsUsers)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
