using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class BackgroundJobFailedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<BackgroundJobFailedEvent>
{
    public async Task HandleAsync(BackgroundJobFailedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("admin.system.monitor", ct);
        var recipients = all.ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Arka plan işi başarısız",
            content: $"{@event.JobName} işi {@event.RetryCount}. denemede başarısız: {@event.Error}",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Sistem,
            userIds: recipients,
            actionUrl: $"/admin/system/logs?jobName={@event.JobName}");
    }
}
