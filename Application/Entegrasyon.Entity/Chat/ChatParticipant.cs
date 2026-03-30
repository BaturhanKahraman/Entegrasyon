using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Chat;

public sealed class ChatParticipant
{
    public long ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public bool IsRemoved { get; set; }
}
