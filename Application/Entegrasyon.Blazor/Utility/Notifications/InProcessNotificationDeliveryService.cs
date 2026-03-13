using System.Collections.Concurrent;
using System.Collections.Immutable;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Blazor.Utility.Notifications;

/// <summary>
/// In-process notification delivery: Blazor component'ları subscribe olur,
/// NotificationEventPublisher her event'i bu servis üzerinden iletir.
/// EventChannel'dan doğrudan okuma yapılmaz — tüm consumer'lar bu servisi kullanır.
/// </summary>
public sealed class InProcessNotificationDeliveryService(
    ILogger<InProcessNotificationDeliveryService> logger) : INotificationDeliveryService, INotificationChannel
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
            if (!_subscribers.TryGetValue(userId, out var handlers) || handlers.Count == 0)
                continue;

            foreach (var handler in handlers)
            {
                try
                {
                    await handler(evt);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Notification delivery failed for user {UserId}, notification {NotificationId}",
                        userId, evt.NotificationId);
                }
            }
        }
    }

    // INotificationChannel — SignalR için genişleme noktası
    public Task SendAsync(NotificationEvent evt, IEnumerable<Guid> userIds)
        => DeliverAsync(evt);
}
