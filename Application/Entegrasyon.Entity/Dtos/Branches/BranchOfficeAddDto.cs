namespace Entegrasyon.Entity.Dtos.Branches;

/// <summary>
/// Şube ofisi ekleme DTO'su.
/// AssignedUserIds boş ise kullanıcılara otomatik HQ atanır (bkz. BranchOfficeManager.AddBranch).
/// </summary>
public record BranchOfficeAddDto(
    string Name,
    string? Address = null,
    IReadOnlyList<Guid>? AssignedUserIds = null);
