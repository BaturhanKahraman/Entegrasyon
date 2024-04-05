using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Notifications;
using Shared.Entity;

namespace Entegrasyon.Entity.User;
public class ApplicationUser : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string FullName { get; set; }

    public string Email { get; set; }
    public string UserName { get; set; }
    public string NormalizedUserName { get; set; }
    public string NormalizedEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }
    public bool IsTwoFactorAuthActive { get; set; } = false;
    public bool NeedsTakeNewPassword { get; set; }
    [MaxLength(15)]
    public string TemporaryPassword { get; set; }

    public string WebJwtToken { get; set; }
    public DateTimeOffset WebJwtTokenExpiresAt { get; set; }

    public string MobileJwtToken { get; set; }
    public DateTimeOffset MobileJwtTokenExpiresAt { get; set; }
    public ICollection<Login> Logins { get; set; } = [];
    public ICollection<UsersRoles> UsersRoles { get; set; }
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<UsersClaims> MyProperty { get; set; }
    public ICollection<ApplicationClaim> Claims { get; set; } = [];
    public ICollection<NotificationsUsers> NotificationsUsers { get; set; }
    public ICollection<Notification> Notifications { get; set; } = [];
    public int? DefaultBranchOfficeId { get; set; }
    public BranchOffice DefaultBranchOffice { get; set; }

    public byte[] RowVersion { get; set; }
    public override string ToString()
    {
        return $"{Name} {Surname} {Email}";
    }
}