using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Entity.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class StorefrontProductQuestionNotificationHandlerTests
{
    private readonly Mock<INotificationManager> _notifManager = new();
    private readonly Mock<INotificationRecipientResolver> _resolver = new();

    [Fact]
    public async Task Handle_ResolvesRecipients_CallsSendNotification()
    {
        var user = Guid.NewGuid();
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.questions.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid> { user });

        var sut = new StorefrontProductQuestionNotificationHandler(_notifManager.Object, _resolver.Object);
        var evt = new StorefrontProductQuestionEvent(88L, Guid.NewGuid(), 10);

        await sut.HandleAsync(evt);

        _notifManager.Verify(m => m.SendNotification(
            "Mağaza sorusu",
            It.Is<string>(s => s.Contains("soru")),
            NotificationSeverity.Warning,
            NotificationCategory.Mağaza,
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == user),
            "/storefront/questions/88"), Times.Once);
    }

    [Fact]
    public async Task Handle_NoRecipients_DoesNothing()
    {
        _resolver.Setup(r => r.ResolveByPermissionAsync("storefront.questions.view", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Guid>());

        var sut = new StorefrontProductQuestionNotificationHandler(_notifManager.Object, _resolver.Object);
        await sut.HandleAsync(new StorefrontProductQuestionEvent(1L, Guid.NewGuid(), 1));

        _notifManager.Verify(m => m.SendNotification(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationSeverity>(),
            It.IsAny<NotificationCategory>(), It.IsAny<IEnumerable<Guid>>(), It.IsAny<string?>()),
            Times.Never);
    }
}
