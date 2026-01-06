using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public sealed class NotificationManager(IEnumerable<INotificationSender> notificationSenders, IntegrationDbContext context, IFluentValidator validator) : INotificationManager
{
    public async Task SendNotification(Notification notification, IEnumerable<SenderType> senderTypes)
    {
        await validator.ValidateAndThrowAsync(notification);
        notification.CreatedAt = DateTimeOffset.UtcNow;

        // Duplicate Users'ları kaldır
        if (notification.Users.Any())
        {
            notification.Users = notification.Users.DistinctBy(u => u.Id).ToList();
        }

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        IEnumerable<Guid> userIds = notification.Users.Any()
            ? notification.Users.Select(u => u.Id).Distinct()
            : context.Claims
                .Include(c => c.Users)
                .Where(c => notification.Claims.Contains(c))
                .SelectMany(c => c.Users)
                .Select(u => u.Id)
                .Distinct();

        var targetSenders = notificationSenders.Where(ns => senderTypes.Contains(ns.Type)).ToArray();
        if (userIds.Any() && targetSenders.Length > 0)
        {
            await Task.WhenAll(targetSenders.Select(ns => ns.SendNotification(notification, userIds)));
        }
    }

    public async Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false)
    {
        var query = context.Notifications
            .Include(n => n.Users)
            .Include(n => n.Claims)
            .Where(n => n.Users.Any(u => u.Id == userId) ||
                       n.Claims.Any(c => c.Users.Any(u => u.Id == userId)));

        if (onlyUnread)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task MarkAsRead(long notificationId, Guid userId)
    {
        var notification = await context.Notifications
            .Include(n => n.Users)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.Users.Any(u => u.Id == userId));

        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }
    }

}
