namespace Entegrasyon.Entity.Dtos.Users;

public sealed record UserDetailListDto(
    Guid Id,
    string Name,
    string Surname,
    string UserName,
    bool IsActive,
    bool IsTwoFactorAuthActive,
    bool NeedsTakeNewPassword,
    DateTimeOffset CreatedAt,
    string DefaultOfficeName);


