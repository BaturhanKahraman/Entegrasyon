using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Chat;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Chat;
using Entegrasyon.Entity.Dtos.Chat;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class ChatManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    EventChannel<ChatMessageEvent> eventChannel,
    ITenantContext tenantContext,
    ILogger<ChatManager> logger) : IChatManager
{
    public async Task<ChatConversation> GetOrCreateDirectConversation(Guid userId1, Guid userId2)
    {
        // 1. Validation
        if (userId1 == Guid.Empty || userId2 == Guid.Empty)
            throw new ArgumentException("Kullanıcı ID'leri boş olamaz.");

        if (userId1 == userId2)
            throw new ArgumentException("Kendisiyle sohbet oluşturulamaz.");

        // 2. Business Rules — (yok)

        // 3. Execution
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Her iki kullanıcının da aktif katılımcı olduğu Direct conversation'ı ara
        var existing = await dbContext.ChatConversations
            .Include(c => c.Participants)
            .Where(c => c.Type == ChatConversationType.Direct)
            .Where(c => c.Participants.Any(p => p.UserId == userId1 && !p.IsRemoved))
            .Where(c => c.Participants.Any(p => p.UserId == userId2 && !p.IsRemoved))
            .FirstOrDefaultAsync();

        if (existing is not null)
            return existing;

        var now = DateTimeOffset.UtcNow;
        var conversation = new ChatConversation
        {
            Type = ChatConversationType.Direct,
            CreatedByUserId = userId1,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = userId1, JoinedAt = now },
                new() { UserId = userId2, JoinedAt = now }
            }
        };

        dbContext.ChatConversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Direct conversation oluşturuldu: {ConversationId} ({User1} ↔ {User2})",
            conversation.Id, userId1, userId2);

        return conversation;
    }

    public async Task<ChatMessage> SendMessage(Guid senderId, SendChatMessageDto dto)
    {
        // 1. Validation
        await validator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var participant = await dbContext.ChatParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == dto.ConversationId
                                      && p.UserId == senderId
                                      && !p.IsRemoved);

        if (participant is null)
            throw new InvalidOperationException("Bu sohbete mesaj gönderme yetkiniz yok.");

        // 3. Execution
        var message = new ChatMessage
        {
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            Content = dto.Content,
            MessageType = dto.MessageType,
            ReplyToMessageId = dto.ReplyToMessageId
        };

        dbContext.ChatMessages.Add(message);

        // Gönderenin LastReadAt'ini güncelle — kendi mesajını okumuş sayılır
        participant.LastReadAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();

        // Alıcıları belirle ve event yayınla
        var sender = await dbContext.Users
            .Where(u => u.Id == senderId)
            .Select(u => new { u.FullName })
            .FirstAsync();

        var recipientIds = await dbContext.ChatParticipants
            .Where(p => p.ConversationId == dto.ConversationId && p.UserId != senderId && !p.IsRemoved)
            .Select(p => p.UserId)
            .ToListAsync();

        var evt = new ChatMessageEvent
        {
            MessageId = message.Id,
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            SenderName = sender.FullName ?? "Bilinmeyen",
            Content = dto.Content,
            MessageType = dto.MessageType,
            RecipientUserIds = recipientIds,
            TenantId = tenantContext.TenantId
        };
        await eventChannel.Writer.WriteAsync(evt);

        logger.LogDebug("Mesaj gönderildi: {MessageId} → Conversation {ConversationId}",
            message.Id, dto.ConversationId);

        return message;
    }

    public async Task<List<ChatConversationListItemDto>> GetConversationsForUser(Guid userId, int take = 50)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var conversations = await dbContext.ChatParticipants
            .Where(p => p.UserId == userId && !p.IsRemoved)
            .Select(p => new
            {
                Conversation = p.Conversation,
                MyLastReadAt = p.LastReadAt,
                Participants = p.Conversation.Participants
                    .Where(pp => !pp.IsRemoved)
                    .Select(pp => new ChatParticipantDto(
                        pp.UserId,
                        pp.User.Name ?? "",
                        pp.User.Surname ?? ""))
                    .ToList(),
                LastMessageContent = p.Conversation.Messages
                    .OrderByDescending(m => m.Id)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                LastMessageAt = p.Conversation.Messages
                    .OrderByDescending(m => m.Id)
                    .Select(m => (DateTimeOffset?)m.CreatedAt)
                    .FirstOrDefault(),
                UnreadCount = p.LastReadAt.HasValue
                    ? p.Conversation.Messages.Count(m => m.CreatedAt > p.LastReadAt.Value && m.SenderId != userId)
                    : p.Conversation.Messages.Count(m => m.SenderId != userId)
            })
            .OrderByDescending(x => x.LastMessageAt ?? x.Conversation.CreatedAt)
            .Take(take)
            .ToListAsync();

        return conversations.Select(c =>
        {
            // Direct conversation için karşı tarafın adını göster
            var displayName = c.Conversation.Type == ChatConversationType.Direct
                ? c.Participants
                    .Where(p => p.UserId != userId)
                    .Select(p => $"{p.Name} {p.Surname}")
                    .FirstOrDefault() ?? "Bilinmeyen"
                : c.Conversation.Name ?? "Grup";

            return new ChatConversationListItemDto(
                c.Conversation.Id,
                displayName,
                c.LastMessageContent,
                c.LastMessageAt,
                c.UnreadCount,
                c.Participants);
        }).ToList();
    }

    public async Task<List<ChatMessageDto>> GetMessages(
        long conversationId,
        Guid requestingUserId,
        int take = 50,
        long? beforeMessageId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Kullanıcının katılımcı olduğunu doğrula
        var participant = await dbContext.ChatParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId
                                      && p.UserId == requestingUserId
                                      && !p.IsRemoved);

        if (participant is null)
            return [];

        var query = dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId);

        // Cursor-based pagination
        if (beforeMessageId.HasValue)
            query = query.Where(m => m.Id < beforeMessageId.Value);

        var messages = await query
            .OrderByDescending(m => m.Id)
            .Take(take)
            .Select(m => new ChatMessageDto(
                m.Id,
                m.SenderId,
                m.Sender.FullName ?? "",
                m.Content,
                m.MessageType,
                m.CreatedAt,
                participant.LastReadAt.HasValue && m.CreatedAt <= participant.LastReadAt.Value,
                m.ReplyToMessageId))
            .ToListAsync();

        // Kronolojik sıraya çevir
        messages.Reverse();
        return messages;
    }

    public async Task MarkConversationAsRead(long conversationId, Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        await dbContext.ChatParticipants
            .Where(p => p.ConversationId == conversationId && p.UserId == userId && !p.IsRemoved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.LastReadAt, DateTimeOffset.UtcNow));
    }

    public async Task<int> GetTotalUnreadCount(Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var unreadCount = await dbContext.ChatParticipants
            .Where(p => p.UserId == userId && !p.IsRemoved)
            .SelectMany(p => p.Conversation.Messages
                .Where(m => m.SenderId != userId
                            && (!p.LastReadAt.HasValue || m.CreatedAt > p.LastReadAt.Value)))
            .CountAsync();

        return unreadCount;
    }

    public async Task<List<ApplicationUser>> GetAvailableChatUsers(Guid currentUserId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        return await dbContext.Users
            .Where(u => u.IsActive && u.Id != currentUserId)
            .OrderBy(u => u.Name).ThenBy(u => u.Surname)
            .ToListAsync();
    }
}
