using Entegrasyon.Entity.Help;

namespace Entegrasyon.Entity.Dtos.Help;

/// <summary>Kullanıcının yardım formundan gönderdiği talep.</summary>
public sealed record CreateHelpRequestDto(string Subject, string Message, HelpRequestCategory Category);

/// <summary>Admin paneli liste satırı.</summary>
public sealed record HelpRequestListDto(
    int Id,
    string Subject,
    HelpRequestCategory Category,
    HelpRequestStatus Status,
    string? SenderName,
    DateTimeOffset CreatedAt);

/// <summary>Admin paneli detay görünümü.</summary>
public sealed record HelpRequestDetailDto(
    int Id,
    string Subject,
    string Message,
    HelpRequestCategory Category,
    HelpRequestStatus Status,
    string? SenderName,
    string? SenderEmail,
    DateTimeOffset CreatedAt);
