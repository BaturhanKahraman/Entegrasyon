using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class MarketplaceOrderReceivedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<MarketplaceOrderReceivedEvent>
{
    public async Task HandleAsync(MarketplaceOrderReceivedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("orders.view", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Yeni sipariş",
            content: $"Sipariş #{@event.OrderNumber} — {@event.CustomerName} — {@event.Amount:C}",
            severity: NotificationSeverity.Error,
            category: NotificationCategory.Sipariş,
            userIds: recipients,
            actionUrl: $"/orders/{@event.OrderId}");
    }
}
