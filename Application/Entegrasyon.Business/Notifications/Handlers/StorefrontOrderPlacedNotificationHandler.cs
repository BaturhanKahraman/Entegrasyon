using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class StorefrontOrderPlacedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<StorefrontOrderPlacedEvent>
{
    public async Task HandleAsync(StorefrontOrderPlacedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("storefront.orders.view", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Mağaza siparişi",
            content: $"Yeni sipariş: #{@event.OrderId} — {@event.Total:C}",
            severity: NotificationSeverity.Error,
            category: NotificationCategory.Sipariş,
            userIds: recipients,
            actionUrl: $"/storefront/orders/{@event.OrderId}");
    }
}
