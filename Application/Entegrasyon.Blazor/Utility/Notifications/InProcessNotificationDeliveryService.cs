using System.Collections.Concurrent;
using System.Collections.Immutable;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;

namespace Entegrasyon.Blazor.Utility.Notifications;

/// <summary>
/// In-process notification delivery: Blazor component'ları subscribe olur,
/// NotificationEventPublisher her event'i bu servis üzerinden iletir.
/// EventChannel'dan doğrudan okuma yapılmaz — tüm consumer'lar bu servisi kullanır.
/// </summary>
public sealed class InProcessNotificationDeliveryService : INotificationDeliveryService, INotificationChannel
{
    private readonly ConcurrentDictionary<Guid, ImmutableList<Func<NotificationEvent, Task>>> _subscribers = new();

    public void Subscribe(Guid userId, Func<NotificationEvent, Task> handler)
        => _subscribers.AddOrUpdate(userId,
            _ => ImmutableList.Create(handler),
            (_, existing) => existing.Add(handler));

    public void Unsubscribe(Guid userId, Func<NotificationEvent, Task> handler)
        => _subscribers.AddOrUpdate(userId,
            _ => ImmutableList<Func<NotificationEvent, Task>>.Empty,
            (_, existing) => existing.Remove(handler));

    public async Task DeliverAsync(NotificationEvent evt)
    {
        foreach (var userId in evt.UserIds)
        {
            if (_subscribers.TryGetValue(userId, out var handlers) && handlers.Count > 0)
                await Task.WhenAll(handlers.Select(h => h(evt)));
        }
    }

    // INotificationChannel — SignalR için genişleme noktası
    public Task SendAsync(NotificationEvent evt, IEnumerable<Guid> userIds)
        => DeliverAsync(evt);
}
