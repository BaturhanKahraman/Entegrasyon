using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Entity.User;
public class ApplicationUser : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string NormalizedUserName { get; set; } = null!;
    public string? NormalizedEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[]? PasswordSalt { get; set; }
    public byte[]? PasswordHash { get; set; }
    public bool IsTwoFactorAuthActive { get; set; } = false;
    public bool NeedsTakeNewPassword { get; set; }
    [MaxLength(15)]
    public string TemporaryPassword { get; set; } = null!;

    public string? WebJwtToken { get; set; }
    public DateTimeOffset WebJwtTokenExpiresAt { get; set; }

    public string? MobileJwtToken { get; set; }
    public DateTimeOffset MobileJwtTokenExpiresAt { get; set; }
    public ICollection<Login> Logins { get; set; } = new List<Login>();
    public ICollection<UsersRoles> UsersRoles { get; set; } = new List<UsersRoles>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<UsersClaims> MyProperty { get; set; } = new List<UsersClaims>();
    public ICollection<NotificationsUsers> NotificationsUsers { get; set; } = new List<NotificationsUsers>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public int? DefaultBranchOfficeId { get; set; }
    public BranchOffice? DefaultBranchOffice { get; set; }

    public byte[]? RowVersion { get; set; }
    public override string ToString()
    {
        return $"{Name} {Surname} {Email}";
    }
}
