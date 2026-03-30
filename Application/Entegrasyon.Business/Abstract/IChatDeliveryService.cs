using Entegrasyon.Business.Channels.Events.Chat;

namespace Entegrasyon.Business.Abstract;

public interface IChatDeliveryService
{
    void Subscribe(Guid userId, Func<ChatMessageEvent, Task> handler);
    void Unsubscribe(Guid userId, Func<ChatMessageEvent, Task> handler);
    Task DeliverAsync(ChatMessageEvent evt);
}
