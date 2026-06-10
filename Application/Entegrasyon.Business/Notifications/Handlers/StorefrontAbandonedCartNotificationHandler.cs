using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class StorefrontAbandonedCartNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<StorefrontAbandonedCartEvent>
{
    public async Task HandleAsync(StorefrontAbandonedCartEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("storefront.orders.view", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Terk edilmiş sepet",
            content: $"Müşteri sepeti bıraktı: {@event.ValueAmount:C}",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Mağaza,
            userIds: recipients,
            actionUrl: $"/storefront/carts/{@event.CartId}");
    }
}
