using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class MarketplaceStockSyncFailedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<MarketplaceStockSyncFailedEvent>
{
    public async Task HandleAsync(MarketplaceStockSyncFailedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("marketplace.manage", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Stok senkronizasyonu başarısız",
            content: $"Ürün stok güncellenemedi: {@event.Error}",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Stok,
            userIds: recipients,
            actionUrl: $"/products/{@event.ProductId}");
    }
}
