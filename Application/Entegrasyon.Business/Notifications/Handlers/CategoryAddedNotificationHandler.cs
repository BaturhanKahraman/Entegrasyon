using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class CategoryAddedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<CategoryAddedEvent>
{
    public async Task HandleAsync(CategoryAddedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("categories.view", ct);
        var recipients = all.Where(id => id != @event.CreatedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Yeni kategori",
            content: $"'{@event.Name}' kategorisi eklendi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: $"/categories/{@event.CategoryId}");
    }
}
