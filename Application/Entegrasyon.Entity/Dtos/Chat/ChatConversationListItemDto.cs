namespace Entegrasyon.Entity.Dtos.Chat;

public sealed record ChatConversationListItemDto(
    long Id,
    string DisplayName,
    string? LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    List<ChatParticipantDto> Participants);

public sealed record ChatParticipantDto(
    Guid UserId,
    string? Name,
    string? Surname);
