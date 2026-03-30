using Entegrasyon.Entity.Chat;
using Entegrasyon.Entity.Dtos.Chat;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Business.Abstract;

public interface IChatManager
{
    Task<ChatConversation> GetOrCreateDirectConversation(Guid userId1, Guid userId2);
    Task<ChatMessage> SendMessage(Guid senderId, SendChatMessageDto dto);
    Task<List<ChatConversationListItemDto>> GetConversationsForUser(Guid userId, int take = 50);
    Task<List<ChatMessageDto>> GetMessages(long conversationId, Guid requestingUserId, int take = 50, long? beforeMessageId = null);
    Task MarkConversationAsRead(long conversationId, Guid userId);
    Task<int> GetTotalUnreadCount(Guid userId);
    Task<List<ApplicationUser>> GetAvailableChatUsers(Guid currentUserId);
}
