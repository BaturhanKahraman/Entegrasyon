using System.Collections.Concurrent;

namespace Entegrasyon.Blazor.Utility.Chat;

public sealed class UserOnlineStatusTracker
{
    private readonly ConcurrentDictionary<Guid, int> _connectionCounts = new();

    public void UserConnected(Guid userId)
        => _connectionCounts.AddOrUpdate(userId, 1, (_, count) => count + 1);

    public void UserDisconnected(Guid userId)
    {
        if (_connectionCounts.TryGetValue(userId, out var count))
        {
            if (count <= 1)
                _connectionCounts.TryRemove(userId, out _);
            else
                _connectionCounts.TryUpdate(userId, count - 1, count);
        }
    }

    public bool IsOnline(Guid userId) => _connectionCounts.ContainsKey(userId);

    public HashSet<Guid> GetOnlineUsers(IEnumerable<Guid> userIds)
        => userIds.Where(IsOnline).ToHashSet();
}
