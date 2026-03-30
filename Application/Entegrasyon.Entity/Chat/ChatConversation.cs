using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Chat;

public sealed class ChatConversation : BaseEntity
{
    public long Id { get; set; }
    public ChatConversationType Type { get; set; }
    public string? Name { get; set; }
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
