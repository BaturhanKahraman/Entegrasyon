using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Blazor.ViewModels.Auth;

public class LoginViewModel
{
    [HiddenInput]
    public string? ReturnUrl { get; set; }

    [DisplayName("Kullanıcı Adı")]
    [Display(Prompt = "Kullanıcı Adı")]
    [DataType(DataType.Text)]
    [MinLength(2,ErrorMessage = "En az 2 karakter girebilirsiniz!")]
    [MaxLength(55,ErrorMessage = "En fazla 55 karakter girebilirsiniz!")]
    [Required(AllowEmptyStrings = false,ErrorMessage = "Kullanıcı Adı gereklidir.")]
    public string? UserName { get; set; }

    [Display(Prompt = "Şifre",Name = "Şifre",Description = "Lütfen şifrenizi girin",AutoGenerateField = true)]
    [DataType(DataType.Password)]
    [MinLength(2,ErrorMessage = "En az 2 karakter girebilirsiniz!")]
    [MaxLength(55,ErrorMessage = "En fazla 55 karakter girebilirsiniz!")]
    [Required(AllowEmptyStrings = false,ErrorMessage = "Şifre gereklidir.")]
    public string? Password { get; set; }
}
