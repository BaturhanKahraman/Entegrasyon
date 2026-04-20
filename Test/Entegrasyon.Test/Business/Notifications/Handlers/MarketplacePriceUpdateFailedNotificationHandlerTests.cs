using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class MarketplacePriceUpdateFailedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("marketplace.manage", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new MarketplacePriceUpdateFailedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new MarketplacePriceUpdateFailedEvent(1, productId, "Fiyat aralığı hatası");

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Fiyat güncellemesi başarısız",
            It.Is<string>(s => s.Contains("Fiyat aralığı hatası")),
            NotificationSeverity.Warning,
            NotificationCategory.Pazaryeri,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            $"/products/{productId}"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("marketplace.manage", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new MarketplacePriceUpdateFailedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new MarketplacePriceUpdateFailedEvent(1, Guid.NewGuid(), "err"));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
