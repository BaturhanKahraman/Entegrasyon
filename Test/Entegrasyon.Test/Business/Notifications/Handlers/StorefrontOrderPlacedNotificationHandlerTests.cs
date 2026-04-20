using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class StorefrontOrderPlacedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new StorefrontOrderPlacedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new StorefrontOrderPlacedEvent(555L, 10, 149.99m);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Mağaza siparişi",
            It.Is<string>(s => s.Contains("555")),
            NotificationSeverity.Error,
            NotificationCategory.Sipariş,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/storefront/orders/555"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new StorefrontOrderPlacedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new StorefrontOrderPlacedEvent(1L, 1, 0m));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
