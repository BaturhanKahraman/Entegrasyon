using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class StorefrontReviewSubmittedNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.reviews.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new StorefrontReviewSubmittedNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new StorefrontReviewSubmittedEvent(123L, Guid.NewGuid(), 5);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Yeni ürün değerlendirmesi",
            It.Is<string>(s => s.Contains("5")),
            NotificationSeverity.Info,
            NotificationCategory.Magaza,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/storefront/reviews/123"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.reviews.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new StorefrontReviewSubmittedNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new StorefrontReviewSubmittedEvent(1L, Guid.NewGuid(), 1));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
