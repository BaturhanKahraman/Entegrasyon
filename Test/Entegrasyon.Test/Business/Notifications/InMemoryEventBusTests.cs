using System.Threading.Channels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Channels.Events.Notifications;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications;

public class InMemoryEventBusTests
{
    private readonly Channel<BaseEvent> _channel =
        Channel.CreateBounded<BaseEvent>(new BoundedChannelOptions(100));
    private readonly Mock<ITenantContext> _tenantContext = new();

    public InMemoryEventBusTests() => _tenantContext.Setup(t => t.TenantId).Returns(42);

    [Fact]
    public async Task PublishAsync_Ephemeral_WritesToChannel()
    {
        var bus = new InMemoryEventBus(_channel, _tenantContext.Object);
        var evt = new NotificationReadEvent(1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        await bus.PublishAsync(evt, persistent: false);

        var read = await _channel.Reader.ReadAsync();
        read.Should().BeSameAs(evt);
        read.TenantId.Should().Be(42);
    }

    [Fact]
    public async Task PublishAsync_Persistent_Throws()
    {
        var bus = new InMemoryEventBus(_channel, _tenantContext.Object);
        var evt = new NotificationReadEvent(1, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Func<Task> act = async () => await bus.PublishAsync(evt, persistent: true).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AddDomainEvent*");
    }
}
