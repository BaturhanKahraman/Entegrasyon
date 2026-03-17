using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Users;

public record UserEditDto
{
    [Required]
    public Guid Id { get; init; }    
    [Required(ErrorMessage = "Lütfen kullanıcı adı bölümünü girin.")]
    public string UserName { get; init; } = null!;
    [EmailAddress]
    public string Email { get; init; } = null!;
    public bool IsTwoFactorEnabled { get; init; }

    [StringLength(maximumLength: 60,ErrorMessage = "En fazla 60 karakterden oluşabilir")]
    public string Name { get; init; } = null!;
    [StringLength(maximumLength: 60,ErrorMessage = "En fazla 60 karakterden oluşabilir")]
    public string Surname { get; init; } = null!;

    public bool IsActive { get; init; } = true;

    public int? BranchOfficeId { get; init; }

    public ICollection<int> RoleIds { get; init; } = [];
}
/*
 *
 *
 *
 */