using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class ProductUpdatedNotificationHandlerTests
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

        var sut = new ProductUpdatedNotificationHandler(_notifManager.Object, _resolver.Object);
        var productId = Guid.NewGuid();
        var evt = new ProductUpdatedEvent(productId, "Test Ürün", false, actor);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Ürün güncellendi",
            It.Is<string>(s => s.Contains("Test Ürün")),
            NotificationSeverity.Info,
            NotificationCategory.Urun,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == other),
            $"/products/{productId}"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipientsAfterActorFilter_DoesNothing()
    {
        var actor = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("products.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor });

        var sut = new ProductUpdatedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new ProductUpdatedEvent(Guid.NewGuid(), "X", false, actor));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
