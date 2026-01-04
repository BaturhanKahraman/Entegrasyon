using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.ViewModels.User;

public class UserEditViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Lütfen kullanıcı adı bölümünü girin.")]
    public string UserName { get; set; }

    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [MaxLength(55,ErrorMessage = "En fazla 55 karakterden oluşabilir.")]
    [MinLength(2,ErrorMessage = "En az 2 karakterden oluşabilir.")]
    public string Name { get; set; }
    
    [MaxLength(55,ErrorMessage = "En fazla 55 karakterden oluşabilir.")]
    [MinLength(2,ErrorMessage = "En az 2 karakterden oluşabilir.")]
    public string Surname { get; set; }

    [Required(ErrorMessage = "Lütfen kullanıcının ekleneceği ofisi seçin.")]
    [Display(Name = "Ofis/Depo")]
    public int? BranchOfficeId { get; set; }

    public List<SelectListItem> BranchOffices { get; set; }
    [Required(ErrorMessage = "Lütfen kullanıcının rolünü seçin.")]
    [Display(Name = "Rol")]
    public int? RoleId { get; set; }
    public List<SelectListItem> Roles { get; set; }
}