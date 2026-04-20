using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class ProductDeletedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<ProductDeletedEvent>
{
    public async Task HandleAsync(ProductDeletedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("products.view", ct);
        var recipients = all.Where(id => id != @event.DeletedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Ürün silindi",
            content: $"'{@event.ProductTitle}' silindi.",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: null);
    }
}
