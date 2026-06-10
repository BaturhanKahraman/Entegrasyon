using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class StorefrontNewCustomerNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<StorefrontNewCustomerEvent>
{
    public async Task HandleAsync(StorefrontNewCustomerEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("storefront.customers.view", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Yeni müşteri",
            content: $"Yeni kayıt: {@event.Email}",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Mağaza,
            userIds: recipients,
            actionUrl: $"/storefront/customers/{@event.CustomerId}");
    }
}
