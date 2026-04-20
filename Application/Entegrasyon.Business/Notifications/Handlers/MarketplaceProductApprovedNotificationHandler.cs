using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class MarketplaceProductApprovedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<MarketplaceProductApprovedEvent>
{
    public async Task HandleAsync(MarketplaceProductApprovedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("marketplace.manage", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Ürün onaylandı",
            content: "Pazaryerinde ürün onaylandı.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Pazaryeri,
            userIds: recipients,
            actionUrl: $"/products/{@event.ProductId}");
    }
}
