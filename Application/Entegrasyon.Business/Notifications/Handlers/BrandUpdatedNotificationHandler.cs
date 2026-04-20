using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Brands;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class BrandUpdatedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<BrandUpdatedEvent>
{
    public async Task HandleAsync(BrandUpdatedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("brands.view", ct);
        var recipients = all.Where(id => id != @event.UpdatedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Marka güncellendi",
            content: $"'{@event.Name}' güncellendi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: $"/brands/{@event.BrandId}");
    }
}
