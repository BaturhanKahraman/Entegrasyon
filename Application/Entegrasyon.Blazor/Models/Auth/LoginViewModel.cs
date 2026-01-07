using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
