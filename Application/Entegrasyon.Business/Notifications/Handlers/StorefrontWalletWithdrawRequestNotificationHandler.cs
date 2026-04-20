using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class StorefrontWalletWithdrawRequestNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<StorefrontWalletWithdrawRequestEvent>
{
    public async Task HandleAsync(StorefrontWalletWithdrawRequestEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("storefront.wallet.manage", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Cüzdan çekim talebi",
            content: $"Müşteri {@event.Amount:C} çekim talebinde bulundu.",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Magaza,
            userIds: recipients,
            actionUrl: $"/storefront/wallet/requests/{@event.CustomerId}");
    }
}
