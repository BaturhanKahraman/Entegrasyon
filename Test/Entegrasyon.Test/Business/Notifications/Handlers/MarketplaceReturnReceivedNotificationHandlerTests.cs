using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class MarketplaceReturnReceivedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new MarketplaceReturnReceivedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new MarketplaceReturnReceivedEvent(1, 200L, 999L, "Hasarlı geldi");

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "İade talebi",
            It.Is<string>(s => s.Contains("999") && s.Contains("Hasarlı geldi")),
            NotificationSeverity.Warning,
            NotificationCategory.Sipariş,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/orders/999"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("orders.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new MarketplaceReturnReceivedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new MarketplaceReturnReceivedEvent(1, 1L, 1L, "X"));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
