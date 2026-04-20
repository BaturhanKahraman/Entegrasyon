using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class MarketplacePriceUpdateFailedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<MarketplacePriceUpdateFailedEvent>
{
    public async Task HandleAsync(MarketplacePriceUpdateFailedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("marketplace.manage", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Fiyat güncellemesi başarısız",
            content: $"Ürün fiyatı güncellenemedi: {@event.Error}",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Pazaryeri,
            userIds: recipients,
            actionUrl: $"/products/{@event.ProductId}");
    }
}
