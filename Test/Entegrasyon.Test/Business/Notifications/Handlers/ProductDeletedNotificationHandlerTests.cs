using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class ProductDeletedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_ExcludesActor_CallsSendNotification()
    {
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("products.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor, other });

        var sut = new ProductDeletedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new ProductDeletedEvent(Guid.NewGuid(), "Silinecek Ürün", actor);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Ürün silindi",
            It.Is<string>(s => s.Contains("Silinecek Ürün")),
            NotificationSeverity.Warning,
            NotificationCategory.Urun,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == other),
            null), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipientsAfterActorFilter_DoesNothing()
    {
        var actor = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("products.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor });

        var sut = new ProductDeletedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new ProductDeletedEvent(Guid.NewGuid(), "X", actor));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
