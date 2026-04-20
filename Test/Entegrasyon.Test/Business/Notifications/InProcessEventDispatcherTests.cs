using System.Threading.Channels;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications.Handlers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Entegrasyon.UnitTest.Business.Notifications;

public class InProcessEventDispatcherTests
{
    [Fact]
    public async Task Dispatcher_ReadsChannel_InvokesHandler()
    {
        var channel = Channel.CreateBounded<BaseEvent>(10);
        var handler = new SpyHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<NotificationReadEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var sut = new InProcessEventDispatcher(channel, scopeFactory, NullLogger<InProcessEventDispatcher>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await sut.StartAsync(cts.Token);

        var evt = new NotificationReadEvent(1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        await channel.Writer.WriteAsync(evt, cts.Token);
        await Task.Delay(200, cts.Token);
        await sut.StopAsync(CancellationToken.None);

        handler.CallCount.Should().Be(1);
    }

    private sealed class SpyHandler : IDomainEventHandler<NotificationReadEvent>
    {
        public int CallCount { get; private set; }
        public Task HandleAsync(NotificationReadEvent @event, CancellationToken ct = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
