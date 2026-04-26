using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Entegrasyon.Business.Notifications.Sse;

public sealed class SseConnectionRegistry : ISseConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<SseNotificationPayload>>> _byUser = new();

    public Guid Register(Guid userId, Channel<SseNotificationPayload> channel)
    {
        var connId = Guid.NewGuid();
        var map = _byUser.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<SseNotificationPayload>>());
        map[connId] = channel;
        return connId;
    }

    public void Unregister(Guid userId, Guid connectionId)
    {
        if (_byUser.TryGetValue(userId, out var map))
        {
            map.TryRemove(connectionId, out _);
            if (map.IsEmpty) _byUser.TryRemove(userId, out _);
        }
    }

    public IReadOnlyList<Channel<SseNotificationPayload>> GetChannels(Guid userId)
        => _byUser.TryGetValue(userId, out var map) ? map.Values.ToList() : [];

    public int GetActiveConnectionCount(Guid userId)
        => _byUser.TryGetValue(userId, out var map) ? map.Count : 0;
}
