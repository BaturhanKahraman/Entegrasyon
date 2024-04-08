using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class NotificationManager(IEnumerable<INotificationSender> notificationSenders, IntegrationDbContext context, FluentValidator validator)
{
    public async Task SendNotification(Notification notification, IEnumerable<SenderType> senderTypes)
    {
        await validator.ValidateAndThrowAsync(notification);
        notification.CreatedAt = DateTimeOffset.UtcNow;

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        IEnumerable<Guid> userIds = notification.Users.Any()
            ? notification.Users.Select(u => u.Id)
            : context.Claims.Include(c => c.Users).Where(c => notification.Claims.Contains(c)).SelectMany(c => c.Users)
                .Select(u => u.Id);

        Parallel.ForEach(notificationSenders.Where(ns => senderTypes.Contains(ns.Type)), async notificationSender =>
        {
            if (userIds is not null)
                await notificationSender.SendNotification(notification, userIds);
        });
    }

}