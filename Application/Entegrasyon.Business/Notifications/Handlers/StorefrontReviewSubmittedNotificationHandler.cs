using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class StorefrontReviewSubmittedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<StorefrontReviewSubmittedEvent>
{
    public async Task HandleAsync(StorefrontReviewSubmittedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("storefront.reviews.view", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Yeni ürün değerlendirmesi",
            content: $"{@event.Rating} yıldızlı değerlendirme geldi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Mağaza,
            userIds: recipients,
            actionUrl: $"/storefront/reviews/{@event.ReviewId}");
    }
}
