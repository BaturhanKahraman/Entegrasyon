using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.Profile.ViewModels;

public class ChangePasswordVm
{
    [Required(ErrorMessage = "Mevcut Şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [MinLength(4, ErrorMessage = "Yeni Şifre en az 4 karakter olmalidir.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrar zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eslesmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
