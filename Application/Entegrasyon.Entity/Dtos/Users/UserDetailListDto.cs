namespace Entegrasyon.Entity.Dtos.Users;

public sealed record UserDetailListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string UserName { get; set; }
    public bool IsActive { get; set; }
    public bool IsTwoFactorAuthActive { get; set; }
    public bool NeedsTakeNewPassword { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string DefaultOfficeName { get; set; }
}