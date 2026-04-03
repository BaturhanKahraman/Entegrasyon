namespace Entegrasyon.MVC.Features.Auth.ViewModels;

public class PasswordResetVm
{
    public string? UserId { get; set; }
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
