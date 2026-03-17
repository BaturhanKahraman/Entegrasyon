namespace Entegrasyon.Entity.Dtos.Users;

public sealed record UserEditDetailDto(
    Guid Id,
    string Name,
    string Surname,
    string UserName,
    string Email,
    bool IsActive,
    bool IsTwoFactorAuthActive,
    bool NeedsTakeNewPassword,
    int? DefaultBranchOfficeId,
    string DefaultOfficeName,
    List<int> RoleIds,
    string RoleName,
    DateTimeOffset CreatedAt);
