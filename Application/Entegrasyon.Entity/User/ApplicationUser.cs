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

    /// <summary>
    /// Oturum geçersizleştirme damgası. Pasifleştirme/silme/şifre sıfırlama gibi güvenlik-kritik
    /// işlemlerde değiştirilir (bump). Login'de cookie'ye claim olarak yazılır; her authenticated
    /// istekte DB'deki değerle karşılaştırılır — eşleşmezse oturum düşürülür (auto-logout).
    /// </summary>
    [MaxLength(32)]
    public string? SecurityStamp { get; set; }
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
    /// <summary>
    /// Kullanıcının birincil/atanmış şube ofisi. Login sonrası aktif ofis fallback sıralamasında
    /// LastSelected (Remember me) → DefaultBranchOffice → HQ olarak kullanılır.
    /// Many-to-many ilişkinin bir alt kümesi; asıl atama listesi UserBranchOffices junction'ında.
    /// </summary>
    public int? DefaultBranchOfficeId { get; set; }
    public BranchOffice? DefaultBranchOffice { get; set; }

    /// <summary>
    /// "Beni hatırla" ile kaydedilen son seçili aktif şube (persist edilmiş).
    /// Sadece RememberLastBranchOffice=true ise fallback'te kullanılır.
    /// Ofis silindiğinde Stage B tarafından temizlenir.
    /// </summary>
    public int? LastSelectedBranchOfficeId { get; set; }
    public BranchOffice? LastSelectedBranchOffice { get; set; }

    /// <summary>
    /// Kullanıcı "Beni hatırla" kutusunu işaretlediyse true; LastSelectedBranchOfficeId fallback'te kullanılır.
    /// </summary>
    public bool RememberLastBranchOffice { get; set; }

    /// <summary>Kullanıcının atanmış olduğu tüm şube ofisleri (many-to-many junction).</summary>
    public ICollection<UserBranchOffice> UserBranchOffices { get; set; } = new List<UserBranchOffice>();

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
