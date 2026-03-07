using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public sealed class NotificationManager(IEnumerable<INotificationSender> notificationSenders, IntegrationDbContext context, IFluentValidator validator) : INotificationManager
{
    public async Task SendNotification(Notification notification, IEnumerable<SenderType> senderTypes)
    {
        await validator.ValidateAndThrowAsync(notification);
        notification.CreatedAt = DateTimeOffset.UtcNow;

        // Ensure Users collection exists
        if (notification.Users == null)
        {
            notification.Users = new List<ApplicationUser>();
        }

        // Deduplicate incoming users by Id
        var incomingUserIds = notification.Users
            .Where(u => u != null && u.Id != Guid.Empty)
            .Select(u => u.Id)
            .Distinct()
            .ToList();

        // Resolve existing users from the DB and attach them to avoid EF trying to INSERT duplicates
        List<ApplicationUser> trackedUsers = new();
        if (incomingUserIds.Any())
        {
            trackedUsers = await context.Users
                .Where(u => incomingUserIds.Contains(u.Id))
                .ToListAsync();
        }

        // Replace notification.Users with the tracked entities (only link existing users)
        notification.Users = trackedUsers;

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        IEnumerable<Guid> userIds = notification.Users?.Select(u => u.Id).Distinct() ?? Enumerable.Empty<Guid>();

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
            .Where(n => n.Users.Any(u => u.Id == userId));

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
