using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Chat;

public sealed class ChatMessage : BaseEntity
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public Guid SenderId { get; set; }
    public ApplicationUser Sender { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public ChatMessageType MessageType { get; set; }
    public long? ReplyToMessageId { get; set; }
    public ChatMessage? ReplyToMessage { get; set; }
}
