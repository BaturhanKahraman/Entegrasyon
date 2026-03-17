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
    public string NormalizedEmail { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public byte[] PasswordSalt { get; set; } = null!;
    public byte[] PasswordHash { get; set; } = null!;
    public bool IsTwoFactorAuthActive { get; set; } = false;
    public bool NeedsTakeNewPassword { get; set; }
    [MaxLength(15)]
    public string TemporaryPassword { get; set; } = null!;

    public string WebJwtToken { get; set; } = null!;
    public DateTimeOffset WebJwtTokenExpiresAt { get; set; }

    public string MobileJwtToken { get; set; } = null!;
    public DateTimeOffset MobileJwtTokenExpiresAt { get; set; }
    public ICollection<Login> Logins { get; set; } = [];
    public ICollection<UsersRoles> UsersRoles { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<UsersClaims> MyProperty { get; set; } = [];
    public ICollection<NotificationsUsers> NotificationsUsers { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public int? DefaultBranchOfficeId { get; set; }
    public BranchOffice? DefaultBranchOffice { get; set; }

    public byte[] RowVersion { get; set; } = null!;
    public override string ToString()
    {
        return $"{Name} {Surname} {Email}";
    }
}
