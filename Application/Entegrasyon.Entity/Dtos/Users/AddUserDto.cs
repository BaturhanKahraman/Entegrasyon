using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Users;

public sealed record AddUserDto
{
    [Required(ErrorMessage = "Lütfen kullanıcı adı bölümünü girin.")]
    public string UserName { get; init; }
    public string Email { get; init; }
    public bool IsTwoFactorEnabled { get; init; }
    [Required(ErrorMessage = "Lütfen geçici şifre kısmını doldurun."),MaxLength(10)]
    public string TemporaryPassword { get; init; }

    [StringLength(maximumLength: 55,ErrorMessage = "En fazla 55 karakterden oluşabilir")]
    public string Name { get; init; }
    [StringLength(maximumLength: 80,ErrorMessage = "En fazla 80 karakterden oluşabilir")]
    public string Surname { get; init; }

    [Required(ErrorMessage = "Lütfen kullanıcının ekleneceği ofisi seçin.")]
    public int BranchOfficeId { get; init; }
    public int RoleId { get; set; }
}