using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class MarketplaceOrderReceivedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user1, user2 });

        var sut = new MarketplaceOrderReceivedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new MarketplaceOrderReceivedEvent(1, 100L, "ORD-001", "Ali Veli", 299.99m);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Yeni sipariş",
            It.Is<string>(s => s.Contains("ORD-001") && s.Contains("Ali Veli")),
            NotificationSeverity.Error,
            NotificationCategory.Sipariş,
            It.Is<IEnumerable<Guid>>(ids => ids.Count() == 2),
            "/orders/100"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new MarketplaceOrderReceivedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new MarketplaceOrderReceivedEvent(1, 1L, "X", "Y", 0m));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
