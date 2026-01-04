using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Blazor.ViewModels.Auth;

public class CreatePasswordViewModel
{
    [Required]
    [HiddenInput]
    public Guid UserId { get; set; }

    [Display(Prompt = "Şifre",Name = "Şifre",Description = "Lütfen şifrenizi girin")]
    [DataType(DataType.Password)]
    [MinLength(2,ErrorMessage = "En az 2 karakter girebilirsiniz!")]
    [MaxLength(55,ErrorMessage = "En fazla 55 karakter girebilirsiniz!")]
    [Required(AllowEmptyStrings = false,ErrorMessage = "Şifre gereklidir.")]
    public string Password { get; set; }

    [Compare(nameof(Password),ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Prompt = "Lütfen şifrenizi tekrar girin",Name = "Şifre Tekrar",Description = "Lütfen şifrenizi tekrar girin.")]
    [DataType(DataType.Password)]
    [MinLength(2,ErrorMessage = "En az 2 karakter girebilirsiniz!")]
    [MaxLength(55,ErrorMessage = "En fazla 55 karakter girebilirsiniz!")]
    public string ComparePassword { get; set; }
}