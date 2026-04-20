using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class BackgroundJobFailedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("admin.system.monitor", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new BackgroundJobFailedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new BackgroundJobFailedEvent("TrendyolSync", "Connection refused", 3);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Arka plan işi başarısız",
            It.Is<string>(s => s.Contains("TrendyolSync") && s.Contains("3")),
            NotificationSeverity.Warning,
            NotificationCategory.Sistem,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/admin/system/logs?jobName=TrendyolSync"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("admin.system.monitor", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new BackgroundJobFailedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new BackgroundJobFailedEvent("Job", "err", 1));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
