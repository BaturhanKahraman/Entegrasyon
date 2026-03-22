using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.AdminPanel.Features.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola gereklidir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni Hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
