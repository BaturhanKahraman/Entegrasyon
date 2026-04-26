using System.Threading.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Business.Notifications.Sse;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class NotificationDismissedSyncHandlerTests
{
    private readonly Mock<ISseConnectionRegistry> _registry = new();

    [Fact]
    public async Task HandleAsync_WritesDismissedPayloadToAllUserChannels()
    {
        var userId = Guid.NewGuid();
        var notificationId = 99L;

        var ch1 = Channel.CreateUnbounded<SseNotificationPayload>();
        var ch2 = Channel.CreateUnbounded<SseNotificationPayload>();

        _registry.Setup(r => r.GetChannels(userId))
                 .Returns(new List<Channel<SseNotificationPayload>> { ch1, ch2 });

        var sut = new NotificationDismissedSyncHandler(_registry.Object);
        var evt = new NotificationDismissedEvent(notificationId, userId);

        await sut.HandleAsync(evt);

        ch1.Reader.TryRead(out var payload1).Should().BeTrue();
        payload1.Should().NotBeNull();
        payload1!.EventType.Should().Be("notification.dismissed");
        payload1.NotificationId.Should().Be(notificationId);
        payload1.ReadAt.Should().BeNull();

        ch2.Reader.TryRead(out var payload2).Should().BeTrue();
        payload2!.EventType.Should().Be("notification.dismissed");
    }

    [Fact]
    public async Task HandleAsync_NoConnections_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        _registry.Setup(r => r.GetChannels(userId))
                 .Returns(new List<Channel<SseNotificationPayload>>());

        var sut = new NotificationDismissedSyncHandler(_registry.Object);
        var act = () => sut.HandleAsync(new NotificationDismissedEvent(1, userId));

        await act.Should().NotThrowAsync();
    }
}
