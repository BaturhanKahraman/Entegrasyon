using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Users;

public sealed record AddUserDto
{
    [Required(ErrorMessage = "Lütfen kullanıcı adı bölümünü girin.")]
    public string UserName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public bool IsTwoFactorEnabled { get; init; }
    [Required(ErrorMessage = "Lütfen geçici şifre kısmını doldurun."),MaxLength(10)]
    public string TemporaryPassword { get; init; } = null!;

    [StringLength(maximumLength: 60,ErrorMessage = "En fazla 60 karakterden oluşabilir")]
    public string Name { get; init; } = null!;
    [StringLength(maximumLength: 60,ErrorMessage = "En fazla 60 karakterden oluşabilir")]
    public string Surname { get; init; } = null!;

    public int? BranchOfficeId { get; init; }
    public int RoleId { get; set; }
}