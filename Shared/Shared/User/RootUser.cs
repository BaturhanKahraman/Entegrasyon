using System.ComponentModel.DataAnnotations;
using Shared.Entity;
using Shared.User.Token;

namespace Shared.User;

public class RootUser : GuidEntity
{
    [Required, StringLength(maximumLength: 80,MinimumLength = 1)]
    public string Name { get; set; }
    [Required, StringLength(maximumLength: 55,MinimumLength = 1)]
    public string Surname { get; set; }
    [Required, StringLength(maximumLength: 100)]
    public string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }

    public bool IsMultipleLoginActive { get; set; } = false;
    public bool IsTwoFactorAuthActive { get; set; } = false;
    public bool NeedsTakeNewPassword { get; set; }
    [MaxLength(15)]
    public string TemporaryPassword { get; set; }
    public List<RootLogin> Logins { get; set; }
    public List<RootRole> Roles { get; set; }
    public List<RootJwtToken> JwtTokens { get; set; }
    public override string ToString()
    {
        return $"{Name} {Surname} {Email}";
    }
}