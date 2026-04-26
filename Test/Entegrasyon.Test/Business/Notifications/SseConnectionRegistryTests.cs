using System.Threading.Channels;
using Entegrasyon.Business.Notifications.Sse;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications;

public class SseConnectionRegistryTests
{
    [Fact]
    public void Register_ThenGetChannels_ReturnsRegistered()
    {
        var sut = new SseConnectionRegistry();
        var userId = Guid.NewGuid();
        var ch1 = Channel.CreateBounded<SseNotificationPayload>(10);
        var ch2 = Channel.CreateBounded<SseNotificationPayload>(10);

        sut.Register(userId, ch1);
        sut.Register(userId, ch2);

        sut.GetChannels(userId).Should().HaveCount(2);
    }

    [Fact]
    public void Unregister_RemovesChannel()
    {
        var sut = new SseConnectionRegistry();
        var userId = Guid.NewGuid();
        var ch = Channel.CreateBounded<SseNotificationPayload>(10);
        var id = sut.Register(userId, ch);

        sut.Unregister(userId, id);

        sut.GetChannels(userId).Should().BeEmpty();
    }

    [Fact]
    public void GetActiveConnectionCount_ReflectsRegistrations()
    {
        var sut = new SseConnectionRegistry();
        var userId = Guid.NewGuid();
        sut.Register(userId, Channel.CreateBounded<SseNotificationPayload>(10));
        sut.GetActiveConnectionCount(userId).Should().Be(1);
    }

    [Fact]
    public void GetChannels_UnknownUser_ReturnsEmpty()
    {
        var sut = new SseConnectionRegistry();
        sut.GetChannels(Guid.NewGuid()).Should().BeEmpty();
    }

    [Fact]
    public void TwoUsers_AreIsolated()
    {
        var sut = new SseConnectionRegistry();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        sut.Register(userA, Channel.CreateBounded<SseNotificationPayload>(10));
        sut.Register(userB, Channel.CreateBounded<SseNotificationPayload>(10));

        sut.GetChannels(userA).Should().HaveCount(1);
        sut.GetChannels(userB).Should().HaveCount(1);
    }
}
