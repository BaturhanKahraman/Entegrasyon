using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.User;

namespace Entegrasyon.Blazor.ViewModels.User;

public class UserAddViewModel
{
    [Required(ErrorMessage = "Lütfen kullanıcı adı bölümünü girin.")]
    [Display(Name = "Kullanıcı Adı",Prompt = "Kullanıcı Adınız")]
    public string? UserName { get; set; }

    [DataType(DataType.EmailAddress)]
    [Display(Name = "E-posta",Prompt = "E-posta adresiniz")]
    public string? Email { get; set; }
    [Display(Name = "İki adımlı doğrulama")]
    public bool IsTwoFactorEnabled { get; set; }
    [Required(ErrorMessage = "Kullanıcıya bir geçici şifre atamak zorundasınız.")]
    [DataType(DataType.Password)]
    [Display(Name = "Geçici Şifre")]
    public string? TemporaryPassword { get; set; }
    [Required(ErrorMessage = "Kullanıcıya bir geçici şifre atamak zorundasınız.")]
    [DataType(DataType.Password)]
    [Display(Name = "Geçici Şifre",Prompt = "Lütfen geçici şifreyi tekrar girin")]
    [Compare(nameof(TemporaryPassword),ErrorMessage = "Şifreler eşleşmiyor")]
    public string? TemporaryPasswordCompare { get; set; }
    [Required(ErrorMessage = "İsim bölümü zorunludur!")]
    [Display(Name = "İsim")]
    [MaxLength(55,ErrorMessage = "En fazla 55 karakterden oluşabilir.")]
    [MinLength(2,ErrorMessage = "En az 2 karakterden oluşabilir.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Soyisim bölümü zorunludur!")]
    [Display(Name = "Soyisim")]
    [MaxLength(55,ErrorMessage = "En fazla 55 karakterden oluşabilir.")]
    [MinLength(2,ErrorMessage = "En az 2 karakterden oluşabilir.")]
    public string? Surname { get; set; }

    [Required(ErrorMessage = "Lütfen kullanıcının ekleneceği ofisi seçin.")]
    [Display(Name = "Ofis/Depo")]
    public int? BranchOfficeId { get; set; }

    public List<SelectListItem>? BranchOffices { get; set; }
    [Required(ErrorMessage = "Lütfen kullanıcının rolünü seçin.")]
    [Display(Name = "Rol")]
    public int? RoleId { get; set; }
    public List<SelectListItem>? Roles { get; set; }

}
