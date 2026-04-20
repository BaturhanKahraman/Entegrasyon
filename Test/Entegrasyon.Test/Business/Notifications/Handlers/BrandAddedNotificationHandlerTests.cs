using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Brands;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class BrandAddedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_ExcludesActor_CallsSendNotification()
    {
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("brands.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor, other });

        var sut = new BrandAddedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new BrandAddedEvent(10, "Nike", actor);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Yeni marka",
            It.Is<string>(s => s.Contains("Nike")),
            NotificationSeverity.Info,
            NotificationCategory.Urun,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == other),
            "/brands/10"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipientsAfterActorFilter_DoesNothing()
    {
        var actor = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("brands.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor });

        var sut = new BrandAddedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new BrandAddedEvent(1, "X", actor));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
