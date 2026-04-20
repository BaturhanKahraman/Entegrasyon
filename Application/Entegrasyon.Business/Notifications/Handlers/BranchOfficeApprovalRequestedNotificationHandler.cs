using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Handlers;

public sealed class BranchOfficeApprovalRequestedNotificationHandler(
    INotificationManager notificationManager,
    INotificationRecipientResolver resolver) : IDomainEventHandler<BranchOfficeApprovalRequestedEvent>
{
    public async Task HandleAsync(BranchOfficeApprovalRequestedEvent @event, CancellationToken ct = default)
    {
        var all = await resolver.ResolveByPermissionAsync("branchoffice.approve", ct);
        var recipients = all.Where(id => id != @event.RequestedByUserId).ToList();
        if (recipients.Count == 0) return;

        await notificationManager.SendNotification(
            header: "Şube onayı talebi",
            content: "Yeni şube onayı bekliyor.",
            severity: NotificationSeverity.Warning,
            category: NotificationCategory.Sistem,
            userIds: recipients,
            actionUrl: $"/admin/branch-offices/approvals/{@event.ApprovalId}");
    }
}
