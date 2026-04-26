using System.Threading.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.Business.Notifications.Sse;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications.Handlers;

public class NotificationReadSyncHandlerTests
{
    private readonly Mock<ISseConnectionRegistry> _registry = new();

    [Fact]
    public async Task HandleAsync_WritesReadPayloadToAllUserChannels()
    {
        var userId = Guid.NewGuid();
        var notificationId = 42L;
        var readAt = DateTimeOffset.UtcNow;

        var ch1 = Channel.CreateUnbounded<SseNotificationPayload>();
        var ch2 = Channel.CreateUnbounded<SseNotificationPayload>();

        _registry.Setup(r => r.GetChannels(userId))
                 .Returns(new List<Channel<SseNotificationPayload>> { ch1, ch2 });

        var sut = new NotificationReadSyncHandler(_registry.Object);
        var evt = new NotificationReadEvent(notificationId, userId, readAt);

        await sut.HandleAsync(evt);

        ch1.Reader.TryRead(out var payload1).Should().BeTrue();
        payload1.Should().NotBeNull();
        payload1!.EventType.Should().Be("notification.read");
        payload1.NotificationId.Should().Be(notificationId);
        payload1.ReadAt.Should().Be(readAt);

        ch2.Reader.TryRead(out var payload2).Should().BeTrue();
        payload2!.EventType.Should().Be("notification.read");
    }

    [Fact]
    public async Task HandleAsync_NoConnections_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        _registry.Setup(r => r.GetChannels(userId))
                 .Returns(new List<Channel<SseNotificationPayload>>());

        var sut = new NotificationReadSyncHandler(_registry.Object);
        var act = () => sut.HandleAsync(new NotificationReadEvent(1, userId, DateTimeOffset.UtcNow));

        await act.Should().NotThrowAsync();
    }
}
