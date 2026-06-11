namespace Entegrasyon.MVC.Features.Auth.ViewModels;

public class PasswordResetVm
{
    public string? UserId { get; set; }
    // Knowledge-proof: kullanıcı kendisine atanan geçici şifreyi yeniden girer (IDOR koruması).
    public string TemporaryPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
