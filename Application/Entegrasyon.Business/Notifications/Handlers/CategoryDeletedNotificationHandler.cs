using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class CategoryDeletedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<CategoryDeletedEvent>
{
    public async Task HandleAsync(CategoryDeletedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("categories.view", ct);
        var recipients = all.Where(id => id != @event.DeletedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Kategori silindi",
            content: $"'{@event.Name}' silindi.",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Urun,
            userIds: recipients,
            actionUrl: null);
    }
}
