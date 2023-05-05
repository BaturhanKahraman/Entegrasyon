using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.ViewModels.Auth;

public class ResetPasswordViewModel
{
    [MaxLength(15,ErrorMessage = "15 Karakterden fazla olamaz!")]
    [Required(AllowEmptyStrings =false,ErrorMessage ="Bu alan boş olamaz.")]
    [Display(Name = "Geçici Şifre")]
    public string TemporaryPassword { get; set; }

    [Display(Name = "Geçici Şifre Tekrar")]
    [MaxLength(15,ErrorMessage = "15 Karakterden fazla olamaz!")]
    [Required(AllowEmptyStrings =false,ErrorMessage ="Bu alan boş olamaz.")]
    [Compare(nameof(TemporaryPassword),ErrorMessage = "Şifreler eşleşmiyor.")]
    public string TemporaryPasswordCompare { get; set; }
    
    [HiddenInput]
    public string UserId { get; set; }
    public string UserName { get; set; }
}