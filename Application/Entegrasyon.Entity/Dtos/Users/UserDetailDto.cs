namespace Entegrasyon.Entity.Dtos.Users;

public sealed record UserDetailDto(Guid Id,
    string Name,
    string Surname,
    string UserName,
    bool IsActive,
    bool IsTwoFactorAuthActive,
    bool NeedsTakeNewPassword,
    DateTimeOffset CreatedAt,
    string DefaultOfficeName,
    string RoleName);