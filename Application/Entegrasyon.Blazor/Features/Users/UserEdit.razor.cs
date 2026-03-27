using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Users;

public partial class UserEdit
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private IRoleService RoleService { get; set; } = null!;
    [Inject] private IAuthService AuthService { get; set; } = null!;
    [Inject] private IBranchOfficeManager BranchOfficeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private UserEditDetailDto? _user;
    private bool _loading = true;

    // Edit fields
    private string _editUserName = string.Empty;
    private string _editEmail = string.Empty;
    private string _editName = string.Empty;
    private string _editSurname = string.Empty;
    private IEnumerable<int> _selectedRoleIds = new HashSet<int>();
    private int? _selectedBranchOfficeId;
    private bool _editIsActive;
    private MudForm? _editForm;
    private bool _isSaving;

    // Lookups
    private List<Role> _roles = [];
    private List<BranchOffice> _branchOffices = [];

    // Password reset
    private string _generatedPassword = string.Empty;
    private bool _isResettingPassword;

    protected override async Task OnInitializedAsync()
    {
        await LoadLookups();
        await LoadUser();
    }

    private async Task LoadLookups()
    {
        _roles = (await RoleService.GetRolesSelectList()).ToList();

        var branchResult = await BranchOfficeManager.GetBranchList();
        if (branchResult?.Data is not null)
            _branchOffices = branchResult.Data;
    }

    private async Task LoadUser()
    {
        _loading = true;
        var result = await UserManager.GetUserEditDetail(Id);

        if (result.Success && result.Data is not null)
        {
            _user = result.Data;
            _editUserName = _user.UserName ?? string.Empty;
            _editEmail = _user.Email ?? string.Empty;
            _editName = _user.Name ?? string.Empty;
            _editSurname = _user.Surname ?? string.Empty;
            _selectedRoleIds = new HashSet<int>(_user.RoleIds);
            _selectedBranchOfficeId = _user.DefaultBranchOfficeId;
            _editIsActive = _user.IsActive;
        }

        _loading = false;
    }

    private async Task SaveUser()
    {
        if (_editForm is not null)
            await _editForm.Validate();
        if (_editForm?.IsValid != true) return;

        _isSaving = true;
        try
        {
            var dto = new UserEditDto
            {
                Id = Id,
                UserName = _editUserName.Trim(),
                Email = _editEmail.Trim(),
                Name = _editName.Trim(),
                Surname = _editSurname.Trim(),
                BranchOfficeId = _selectedBranchOfficeId,
                RoleIds = _selectedRoleIds.ToList(),
                IsActive = _editIsActive
            };

            var result = await UserManager.EditUser(dto);
            if (result.Success)
            {
                Snackbar.Add(result.Message ?? "Kullanici guncellendi.", Severity.Success);
                await LoadUser();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Kullanici guncellenemedi.", Severity.Error);
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

    private async Task ResetPassword()
    {
        var confirm = await DialogService.ShowMessageBox(
            "Sifre Sifirla",
            $"{_user?.Name} {_user?.Surname} kullanicisinin sifresini sifirlamak istediginize emin misiniz?",
            yesText: "Sifirla", cancelText: "Iptal");

        if (confirm != true) return;

        _isResettingPassword = true;
        try
        {
            var tempPassword = Guid.NewGuid().ToString()[..8];
            var result = await AuthService.AssignTempPassword(tempPassword, Id.ToString());
            if (result.Success)
            {
                _generatedPassword = tempPassword;
                Snackbar.Add("Gecici sifre basariyla atandi.", Severity.Success);
                await LoadUser();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Sifre sifirlanamadi.", Severity.Error);
            }
        }
        finally
        {
            _isResettingPassword = false;
        }
    }

    private async Task DeleteUser()
    {
        var confirm = await DialogService.ShowMessageBox(
            "Kullanici Sil",
            $"{_user?.Name} {_user?.Surname} kullanicisini silmek istediginize emin misiniz? Bu islem geri alinamaz.",
            yesText: "Sil", cancelText: "Iptal");

        if (confirm != true) return;

        var result = await UserManager.SoftDelete(Id);
        if (result.Success)
        {
            Snackbar.Add(result.Message ?? "Kullanici silindi.", Severity.Success);
            NavigationManager.NavigateTo("/users", replace: true);
        }
        else
        {
            Snackbar.Add(result.Message ?? "Kullanici silinemedi.", Severity.Error);
        }
    }
}
