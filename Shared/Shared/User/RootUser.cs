using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Shared.User;
[Table("Users")]
public class RootUser : GuidEntity
{
    [StringLength(maximumLength: 80)]
    public string Name { get; set; }
    [StringLength(maximumLength: 55)]
    public string Surname { get; set; }
    [StringLength(maximumLength: 100)]
    public string Email { get; set; }
    [StringLength(30)]
    public string UserName { get; set; }
    [StringLength(30)]
    public string NormalizedUserName { get; set; }
    [StringLength(maximumLength: 100)]
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

    public int RoleId { get; set; }
    public RootRole Role { get; set; }

    public List<RootLogin> Logins { get; set; }
    
    public override string ToString()
    {
        return $"{Name} {Surname} {Email}";
    }
}