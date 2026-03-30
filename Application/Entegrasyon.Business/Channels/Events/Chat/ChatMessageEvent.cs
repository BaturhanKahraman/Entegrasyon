using Entegrasyon.Entity.Chat;

namespace Entegrasyon.Business.Channels.Events.Chat;

public sealed class ChatMessageEvent : BaseEvent
{
    public long MessageId { get; init; }
    public long ConversationId { get; init; }
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public ChatMessageType MessageType { get; init; }
    public IEnumerable<Guid> RecipientUserIds { get; init; } = [];
}
