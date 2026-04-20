using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class ProductAddedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<ProductAddedEvent>
{
    public async Task HandleAsync(ProductAddedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("products.view", ct);
        var recipients = all.Where(id => id != @event.ActorUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Yeni ürün eklendi",
            content: $"'{@event.ProductTitle}' adlı ürün eklendi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: $"/products/{@event.ProductId}");
    }
}
