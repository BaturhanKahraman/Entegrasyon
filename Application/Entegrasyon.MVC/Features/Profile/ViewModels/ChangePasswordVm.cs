using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.Profile.ViewModels;

public class ChangePasswordVm
{
    [Required(ErrorMessage = "Mevcut sifre zorunludur.")]
    [DataType(DataType.Password)]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni sifre zorunludur.")]
    [DataType(DataType.Password)]
    [MinLength(4, ErrorMessage = "Yeni sifre en az 4 karakter olmalidir.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre tekrar zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Sifreler eslesmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
