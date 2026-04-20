using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Brands;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class BrandDeletedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<BrandDeletedEvent>
{
    public async Task HandleAsync(BrandDeletedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("brands.view", ct);
        var recipients = all.Where(id => id != @event.DeletedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Marka silindi",
            content: $"'{@event.Name}' silindi.",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: null);
    }
}
