using System.Threading.Channels;

namespace Entegrasyon.Business.Notifications.Sse;

public interface ISseConnectionRegistry
{
    Guid Register(Guid userId, Channel<SseNotificationPayload> channel);
    void Unregister(Guid userId, Guid connectionId);
    IReadOnlyList<Channel<SseNotificationPayload>> GetChannels(Guid userId);
    int GetActiveConnectionCount(Guid userId);
}
