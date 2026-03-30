using Entegrasyon.Entity.Chat;

namespace Entegrasyon.Entity.Dtos.Chat;

public sealed record ChatMessageDto(
    long Id,
    Guid SenderId,
    string? SenderName,
    string Content,
    ChatMessageType MessageType,
    DateTimeOffset CreatedAt,
    bool IsRead,
    long? ReplyToMessageId);
