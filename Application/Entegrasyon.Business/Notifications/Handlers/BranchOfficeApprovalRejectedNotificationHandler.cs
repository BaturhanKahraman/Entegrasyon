using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class BranchOfficeApprovalRejectedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<BranchOfficeApprovalRejectedEvent>
{
    public async Task HandleAsync(BranchOfficeApprovalRejectedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("branchoffice.view", ct);
        var recipients = all.Where(id => id != @event.RejectedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Şube onayı reddedildi",
            content: $"Şube onayı reddedildi: {@event.Reason}",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Sistem,
            userIds: recipients,
            actionUrl: $"/admin/branch-offices/{@event.BranchOfficeId}");
    }
}
