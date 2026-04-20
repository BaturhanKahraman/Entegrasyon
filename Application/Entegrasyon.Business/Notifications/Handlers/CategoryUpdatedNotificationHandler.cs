using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class CategoryUpdatedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<CategoryUpdatedEvent>
{
    public async Task HandleAsync(CategoryUpdatedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("categories.view", ct);
        var recipients = all.Where(id => id != @event.UpdatedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Kategori güncellendi",
            content: $"'{@event.Action}' güncellendi.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: $"/categories/{@event.CategoryId}");
    }
}
