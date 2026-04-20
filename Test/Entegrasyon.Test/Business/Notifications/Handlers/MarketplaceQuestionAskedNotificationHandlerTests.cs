using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class MarketplaceQuestionAskedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ShortQuestion_SendsFullText()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("marketplace.manage", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new MarketplaceQuestionAskedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new MarketplaceQuestionAskedEvent(1, 99L, Guid.NewGuid(), "Bu ürün su geçirmez mi?");

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Yeni müşteri sorusu",
            It.Is<string>(s => s.Contains("su geçirmez")),
            NotificationSeverity.Warning,
            NotificationCategory.Pazaryeri,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/marketplace/1/questions/99"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("marketplace.manage", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new MarketplaceQuestionAskedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new MarketplaceQuestionAskedEvent(1, 1L, Guid.NewGuid(), "Soru?"));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
