using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Entity.User;
public class ApplicationUser : BaseEntity
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? FullName { get; set; }

    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[]? PasswordSalt { get; set; }
    public byte[]? PasswordHash { get; set; }
    public bool IsTwoFactorAuthActive { get; set; } = false;
    public bool NeedsTakeNewPassword { get; set; }
    [MaxLength(15)]
    public string? TemporaryPassword { get; set; }

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

    // Account Lockout
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }

    // Password Hash Version (0 = HMACSHA512 legacy, 1 = bcrypt)
    public int PasswordHashVersion { get; set; }
    public string? BcryptPasswordHash { get; set; }

    // 2FA TOTP
    public string? TwoFactorSecret { get; set; }
    public string? TwoFactorRecoveryCodes { get; set; } // JSON array of SHA256-hashed codes

    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; set; }

    public byte[]? RowVersion { get; set; }
    public override string ToString()
    {
        return $"{Name} {Surname} {Email}";
    }
}
