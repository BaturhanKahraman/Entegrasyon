using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Users;

public partial class Users
{
    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private IRoleService RoleService { get; set; } = null!;
    [Inject] private IAuthService AuthService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private List<UserDetailListDto> _users = [];
    private string _searchString = string.Empty;
    private bool _loading = true;
    private List<Role> _roles = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
        await LoadRoles();
    }

    private async Task LoadRoles()
    {
        _roles = (await RoleService.GetRolesSelectList()).ToList();
    }

    private async Task LoadUsers()
    {
        _loading = true;
        var result = await UserManager.GetPaginatedUserDetails(0, 1000);
        if (result.Success)
        {
            _users = result.Data.Items.ToList();
        }
        else
        {
            Snackbar.Add(result.Message ?? "", Severity.Error);
        }
        _loading = false;
    }

    private async Task OpenAddUserDialog()
    {
        var parameters = new DialogParameters
        {
            { "IsEdit", false },
            { "Roles", _roles }
        };

        var dialog = await DialogService.ShowAsync<UserDialog>("Yeni Kullanıcı", parameters);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is UserDialog.UserDialogResult dialogResult)
        {
            var addUserDto = new AddUserDto
            {
                UserName = dialogResult.Model.UserName,
                Email = dialogResult.Model.Email,
                Name = dialogResult.Model.Name,
                Surname = dialogResult.Model.Surname,
                TemporaryPassword = dialogResult.Password,
                RoleId = dialogResult.RoleIds.FirstOrDefault(),
            };

            var serviceResult = await UserManager.AddUser(addUserDto);
            if (serviceResult.Success)
            {
                Snackbar.Add(serviceResult.Message ?? "", Severity.Success);
                await LoadUsers();
            }
            else
            {
                Snackbar.Add(serviceResult.Message ?? "", Severity.Error);
            }
        }
    }

    private void EditUser(UserDetailListDto user)
    {
        NavigationManager.NavigateTo($"/users/edit/{user.Id}");
    }

    private async Task DeleteUser(UserDetailListDto user)
    {
        var result = await DialogService.ShowMessageBox(
            "Kullanıcı Sil",
            $"{user.Name} kullanıcısını silmek istediğinizden emin misiniz?",
            yesText: "Sil",
            cancelText: "İptal");

        if (result == true)
        {
            var serviceResult = await UserManager.SoftDelete(user.Id);
            if (serviceResult.Success)
            {
                Snackbar.Add(serviceResult.Message ?? "", Severity.Success);
                await LoadUsers();
            }
            else
            {
                Snackbar.Add(serviceResult.Message ?? "", Severity.Error);
            }
        }
    }

    private async Task ResetPassword(UserDetailListDto user)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Şifre Sıfırla",
            $"{user.Name} {user.Surname} kullanıcısının şifresini sıfırlamak istediğinize emin misiniz? Geçici bir şifre atanacak.",
            yesText: "Sıfırla", cancelText: "İptal");

        if (confirm != true) return;

        var tempPassword = Guid.NewGuid().ToString()[..8];
        var result = await AuthService.AssignTempPassword(tempPassword, user.Id.ToString());
        if (result.Success)
        {
            await DialogService.ShowMessageBox(
                "Geçici Şifre",
                $"Geçici şifre: {tempPassword}\n\nKullanıcı bir sonraki girişte yeni şifre oluşturmak zorunda kalacak.",
                yesText: "Tamam");
        }
        else
        {
            Snackbar.Add(result.Message ?? "", Severity.Error);
        }
    }

    private bool FilterFunc(UserDetailListDto user)
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;
        if (user.Name?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true)
            return true;
        if (user.Surname?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true)
            return true;
        if (user.UserName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true)
            return true;
        return false;
    }
}
