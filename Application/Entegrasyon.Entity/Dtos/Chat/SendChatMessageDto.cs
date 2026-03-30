using Entegrasyon.Entity.Chat;

namespace Entegrasyon.Entity.Dtos.Chat;

public sealed record SendChatMessageDto(
    long ConversationId,
    string Content,
    ChatMessageType MessageType = ChatMessageType.Text,
    long? ReplyToMessageId = null);
