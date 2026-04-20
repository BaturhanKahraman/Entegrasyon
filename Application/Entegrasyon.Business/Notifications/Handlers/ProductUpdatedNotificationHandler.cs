using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class ProductUpdatedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<ProductUpdatedEvent>
{
    public async Task HandleAsync(ProductUpdatedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("products.view", ct);
        var recipients = all.Where(id => id != @event.UpdatedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Ürün güncellendi",
            content: $"'{@event.ProductTitle}' güncellendi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: $"/products/{@event.ProductId}");
    }
}
