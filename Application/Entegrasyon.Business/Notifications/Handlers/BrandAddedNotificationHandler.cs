using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Brands;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class BrandAddedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<BrandAddedEvent>
{
    public async Task HandleAsync(BrandAddedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("brands.view", ct);
        var recipients = all.Where(id => id != @event.CreatedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Yeni marka",
            content: $"'{@event.Name}' markası eklendi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: $"/brands/{@event.BrandId}");
    }
}
