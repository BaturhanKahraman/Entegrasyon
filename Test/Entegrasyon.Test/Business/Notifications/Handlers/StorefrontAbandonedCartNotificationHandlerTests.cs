using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class StorefrontAbandonedCartNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new StorefrontAbandonedCartNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new StorefrontAbandonedCartEvent(456L, 20, 89.50m);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Terk edilmiş sepet",
            It.Is<string>(s => s.Contains("sepet")),
            NotificationSeverity.Info,
            NotificationCategory.Magaza,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/storefront/carts/456"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new StorefrontAbandonedCartNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new StorefrontAbandonedCartEvent(1L, 1, 0m));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
