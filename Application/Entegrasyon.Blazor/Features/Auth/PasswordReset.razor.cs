using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Auth;

public partial class PasswordReset
{
    [Inject] private IAuthService AuthService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [SupplyParameterFromQuery]
    public string? UserId { get; set; }

    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isValid;
    private bool _isLoading;

    private IEnumerable<string> PasswordMatch(string arg)
    {
        if (_password != arg)
            yield return "Şifreler eşleşmiyor.";
    }

    private async Task SubmitAsync()
    {
        if (!_isValid) return;

        if (Guid.TryParse(UserId, out var guidId))
        {
            _isLoading = true;
            try
            {
                var result = await AuthService.CreatePassword(_password, guidId);
                if (result.Success)
                {
                    Snackbar.Add(result.Message ?? "", Severity.Success);
                    NavigationManager.NavigateTo("/auth/login");
                }
                else
                {
                    Snackbar.Add(result.Message ?? "", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Bir hata oluştu: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }
        else
        {
            Snackbar.Add("Geçersiz Kullanıcı ID", Severity.Error);
        }
    }
}
