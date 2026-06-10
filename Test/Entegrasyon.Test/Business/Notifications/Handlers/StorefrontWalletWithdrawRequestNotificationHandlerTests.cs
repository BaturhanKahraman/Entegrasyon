using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class StorefrontWalletWithdrawRequestNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.wallet.manage", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new StorefrontWalletWithdrawRequestNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new StorefrontWalletWithdrawRequestEvent(30, 500m);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Cüzdan çekim talebi",
            It.Is<string>(s => s.Contains("çekim")),
            NotificationSeverity.Warning,
            NotificationCategory.Mağaza,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/storefront/wallet/requests/30"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.wallet.manage", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new StorefrontWalletWithdrawRequestNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new StorefrontWalletWithdrawRequestEvent(1, 0m));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
