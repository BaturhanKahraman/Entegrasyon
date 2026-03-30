namespace Entegrasyon.Entity.Chat;

public sealed class ChatMessageReadReceipt
{
    public long MessageId { get; set; }
    public ChatMessage Message { get; set; } = null!;
    public Guid UserId { get; set; }
    public DateTimeOffset ReadAt { get; set; }
}
