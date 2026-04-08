namespace Entegrasyon.Entity.Dtos.Branches;

/// <summary>
/// Şube ofisi düzenleme DTO'su.
/// IsHeadquarters flag'i DTO'da yer almaz — runtime'da asla değişmez (migration-time sabit).
/// </summary>
public record BranchOfficeEditDto(
    int Id,
    string Name,
    string? Address = null,
    IReadOnlyList<Guid>? AssignedUserIds = null);
