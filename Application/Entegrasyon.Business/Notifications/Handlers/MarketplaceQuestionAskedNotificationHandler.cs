using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class MarketplaceQuestionAskedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<MarketplaceQuestionAskedEvent>
{
    public async Task HandleAsync(MarketplaceQuestionAskedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("marketplace.manage", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        var content = @event.QuestionText.Length > 100
            ? @event.QuestionText[..100] + "..."
            : @event.QuestionText;

        await notificationManager.SendNotification(
            header: "Yeni müşteri sorusu",
            content: content,
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Pazaryeri,
            userIds: recipients,
            actionUrl: $"/marketplace/{@event.MarketPlaceId}/questions/{@event.QuestionId}");
    }
}
