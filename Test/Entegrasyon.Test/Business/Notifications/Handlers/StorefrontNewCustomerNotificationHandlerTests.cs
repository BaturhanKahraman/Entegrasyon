using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class StorefrontNewCustomerNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.customers.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new StorefrontNewCustomerNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new StorefrontNewCustomerEvent(77, "test@example.com");

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Yeni müşteri",
            It.Is<string>(s => s.Contains("test@example.com")),
            NotificationSeverity.Info,
            NotificationCategory.Magaza,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/storefront/customers/77"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.customers.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new StorefrontNewCustomerNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new StorefrontNewCustomerEvent(1, "x@x.com"));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
