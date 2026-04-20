using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class MarketplaceReturnReceivedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<MarketplaceReturnReceivedEvent>
{
    public async Task HandleAsync(MarketplaceReturnReceivedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("orders.view", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "İade talebi",
            content: $"Sipariş #{@event.OrderId} için iade: {@event.Reason}",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Sipariş,
            userIds: recipients,
            actionUrl: $"/orders/{@event.OrderId}");
    }
}
