using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class ProductAddedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_ExcludesActor_CallsSendNotification()
    {
        var actor = Guid.NewGuid();
        var other1 = Guid.NewGuid();
        var other2 = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("products.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor, other1, other2 });

        var sut = new ProductAddedNotificationHandler(_notifManager.Object, _resolver.Object);
        var productId = Guid.NewGuid();
        var evt = new ProductAddedEvent(productId, "Test Ürün", actor);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Yeni ürün eklendi",
            It.Is<string>(s => s.Contains("Test Ürün")),
            NotificationSeverity.Info,
            NotificationCategory.Urun,
            It.Is<IEnumerable<Guid>>(ids =>
                ids.Count() == 2
                && !ids.Contains(actor)
                && ids.Contains(other1)
                && ids.Contains(other2)),
            $"/products/{productId}"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipientsAfterActorFilter_DoesNothing()
    {
        var actor = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("products.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { actor });

        var sut = new ProductAddedNotificationHandler(_notifManager.Object, _resolver.Object);

        await sut.HandleAsync(new ProductAddedEvent(Guid.NewGuid(), "X", actor));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
