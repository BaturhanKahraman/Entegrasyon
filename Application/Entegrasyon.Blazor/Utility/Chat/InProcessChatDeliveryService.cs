using System.Collections.Concurrent;
using System.Collections.Immutable;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Chat;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Blazor.Utility.Chat;

/// <summary>
/// In-process chat delivery: Blazor component'ları subscribe olur,
/// ChatEventPublisher her event'i bu servis üzerinden iletir.
/// EventChannel'dan doğrudan okuma yapılmaz — tüm consumer'lar bu servisi kullanır.
///
/// Tenant isolation: database-per-tenant mimarisi kullanıldığı için her tenant'ın
/// kendi user tablosu vardır ve user GUID'leri tenant'lar arasında çakışmaz.
/// Eğer single-DB multi-tenant'a geçilirse, subscriber key'i (int TenantId, Guid UserId)
/// tuple'ına dönüştürülmelidir.
/// </summary>
public sealed class InProcessChatDeliveryService(
    ILogger<InProcessChatDeliveryService> logger) : IChatDeliveryService
{
    private readonly ConcurrentDictionary<Guid, ImmutableList<Func<ChatMessageEvent, Task>>> _subscribers = new();

    public void Subscribe(Guid userId, Func<ChatMessageEvent, Task> handler)
        => _subscribers.AddOrUpdate(userId,
            _ => ImmutableList.Create(handler),
            (_, existing) => existing.Add(handler));

    public void Unsubscribe(Guid userId, Func<ChatMessageEvent, Task> handler)
        => _subscribers.AddOrUpdate(userId,
            _ => ImmutableList<Func<ChatMessageEvent, Task>>.Empty,
            (_, existing) => existing.Remove(handler));

    public async Task DeliverAsync(ChatMessageEvent evt)
    {
        foreach (var userId in evt.RecipientUserIds)
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
                        "Chat delivery failed for user {UserId}, message {MessageId}",
                        userId, evt.MessageId);
                }
            }
        }
    }
}
