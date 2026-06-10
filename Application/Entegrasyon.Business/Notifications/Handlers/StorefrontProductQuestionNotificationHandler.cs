using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class StorefrontProductQuestionNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<StorefrontProductQuestionEvent>
{
    public async Task HandleAsync(StorefrontProductQuestionEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("storefront.questions.view", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Mağaza sorusu",
            content: "Ürüne soru soruldu.",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Mağaza,
            userIds: recipients,
            actionUrl: $"/storefront/questions/{@event.QuestionId}");
    }
}
