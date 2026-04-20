using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class BranchOfficeApprovalApprovedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<BranchOfficeApprovalApprovedEvent>
{
    public async Task HandleAsync(BranchOfficeApprovalApprovedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("branchoffice.view", ct);
        var recipients = all.Where(id => id != @event.ApprovedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Şube onaylandı",
            content: "Şube onaylandı.",
            severity: NotificationSeverity.Info,
            category: NotificationCategory.Sistem,
            userIds: recipients,
            actionUrl: $"/admin/branch-offices/{@event.BranchOfficeId}");
    }
}
