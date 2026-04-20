using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class MarketplaceProductRejectedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<MarketplaceProductRejectedEvent>
{
    public async Task HandleAsync(MarketplaceProductRejectedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("marketplace.manage", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Ürün reddedildi",
            content: $"Ürün reddedildi: {@event.RejectionReason}",
            severity: NotificationSeverity.Error,
            category: NotificationCategory.Pazaryeri,
            userIds: recipients,
            actionUrl: $"/products/{@event.ProductId}");
    }
}
