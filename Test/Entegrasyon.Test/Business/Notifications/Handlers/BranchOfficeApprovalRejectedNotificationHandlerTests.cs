using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class BranchOfficeApprovalRejectedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_ExcludesActor_CallsSendNotification()
    {
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("branchoffice.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor, other });

        var sut = new BranchOfficeApprovalRejectedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new BranchOfficeApprovalRejectedEvent(5, 12, actor, "Belgeler eksik");

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Şube onayı reddedildi",
            It.Is<string>(s => s.Contains("Belgeler eksik")),
            NotificationSeverity.Warning,
            NotificationCategory.Sistem,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == other),
            "/admin/branch-offices/12"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipientsAfterActorFilter_DoesNothing()
    {
        var actor = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("branchoffice.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor });

        var sut = new BranchOfficeApprovalRejectedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new BranchOfficeApprovalRejectedEvent(1, 1, actor, "X"));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
