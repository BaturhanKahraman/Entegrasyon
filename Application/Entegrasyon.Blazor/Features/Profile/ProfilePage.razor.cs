using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Profile;

public partial class ProfilePage
{
    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private IAuthService AuthService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private UserDetailDto? _user;
    private Guid _userId;
    private bool _loading = true;

    // Profile edit fields
    private string _editName = string.Empty;
    private string _editSurname = string.Empty;
    private MudForm? _profileForm;
    private bool _isSaving;

    // Password change fields
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private MudForm? _passwordForm;
    private bool _isChangingPassword;
    private bool _showCurrentPassword;
    private bool _showNewPassword;
    private bool _showConfirmPassword;

    protected override async Task OnInitializedAsync()
    {
        await LoadProfile();
    }

    private async Task LoadProfile()
    {
        _loading = true;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out _userId))
        {
            _loading = false;
            return;
        }

        var result = await UserManager.GetUserDetails(_userId);
        if (result.Success && result.Data is not null)
        {
            _user = result.Data;
            _editName = _user.Name ?? string.Empty;
            _editSurname = _user.Surname ?? string.Empty;
        }

        _loading = false;
    }

    private async Task SaveProfile()
    {
        if (_profileForm is not null)
            await _profileForm.Validate();
        if (_profileForm?.IsValid != true) return;

        _isSaving = true;
        try
        {
            var userId = _userId;
            var dto = new UpdateProfileDto
            {
                Name = _editName.Trim(),
                Surname = _editSurname.Trim()
            };

            var result = await UserManager.UpdateOwnProfile(userId, dto);
            if (result.Success)
            {
                Snackbar.Add("Profil basariyla guncellendi.", Severity.Success);
                await LoadProfile();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Profil guncellenemedi.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task ChangePassword()
    {
        if (_passwordForm is not null)
            await _passwordForm.Validate();
        if (_passwordForm?.IsValid != true) return;

        _isChangingPassword = true;
        try
        {
            var userId = _userId;
            var dto = new ChangePasswordDto
            {
                CurrentPassword = _currentPassword,
                NewPassword = _newPassword,
                ConfirmPassword = _confirmPassword
            };

            var result = await AuthService.ChangeOwnPassword(userId, dto);
            if (result.Success)
            {
                Snackbar.Add("Sifre basariyla degistirildi.", Severity.Success);
                _currentPassword = string.Empty;
                _newPassword = string.Empty;
                _confirmPassword = string.Empty;
                _passwordForm?.ResetValidation();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Sifre degistirilemedi.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isChangingPassword = false;
        }
    }

    private string? ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Yeni sifre zorunludur";
        if (password.Length < 4)
            return "Sifre en az 4 karakter olmalidir";
        if (password.Length > 255)
            return "Sifre en fazla 255 karakter olabilir";
        return null;
    }

    private string? ValidateConfirmPassword(string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(confirmPassword))
            return "Sifre tekrari zorunludur";
        if (confirmPassword != _newPassword)
            return "Sifreler uyusmuyor";
        return null;
    }
}
