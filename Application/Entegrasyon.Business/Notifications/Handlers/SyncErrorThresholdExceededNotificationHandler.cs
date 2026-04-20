using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class SyncErrorThresholdExceededNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<SyncErrorThresholdExceededEvent>
{
    public async Task HandleAsync(SyncErrorThresholdExceededEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("admin.system.monitor", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Sync hata eşiği aşıldı",
            content: $"{@event.ServiceName} son {@event.WindowMinutes} dk'da {@event.ErrorCount} hata verdi.",
            severity: NotificationSeverity.Error,
            category: NotificationCategory.Sistem,
            userIds: recipients,
            actionUrl: "/admin/system/sync");
    }
}
