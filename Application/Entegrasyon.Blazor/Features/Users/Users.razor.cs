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
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

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
            Snackbar.Add(result.Message, Severity.Error);
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
                Snackbar.Add(serviceResult.Message, Severity.Success);
                await LoadUsers();
            }
            else
            {
                Snackbar.Add(serviceResult.Message, Severity.Error);
            }
        }
    }

    private async Task EditUser(UserDetailListDto user)
    {
        var parameters = new DialogParameters
        {
            { "IsEdit", true },
            { "Roles", _roles },
            { "Model", new UserDialog.UserViewModel
                {
                    UserName = user.UserName,
                    Name = user.Name,
                    Surname = user.Surname,
                }
            }
        };

        var detailResult = await UserManager.GetUserDetails(user.Id);
        if (!detailResult.Success)
        {
            Snackbar.Add("Kullanıcı detayları alınamadı", Severity.Error);
            return;
        }

        var fullUser = detailResult.Data;
        parameters["Model"] = new UserDialog.UserViewModel
        {
            UserName = fullUser.UserName,
            Name = fullUser.Name,
            Surname = fullUser.Surname,
            Email = fullUser.UserName
        };

        var dialog = await DialogService.ShowAsync<UserDialog>("Kullanıcı Düzenle", parameters);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is UserDialog.UserDialogResult dialogResult)
        {
            var editDto = new UserEditDto
            {
                Id = user.Id,
                UserName = dialogResult.Model.UserName,
                Email = dialogResult.Model.Email,
            };

            var serviceResult = await UserManager.EditUser(editDto);
            if (serviceResult.Success)
            {
                Snackbar.Add(serviceResult.Message, Severity.Success);
                await LoadUsers();
            }
            else
            {
                Snackbar.Add(serviceResult.Message, Severity.Error);
            }
        }
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
                Snackbar.Add(serviceResult.Message, Severity.Success);
                await LoadUsers();
            }
            else
            {
                Snackbar.Add(serviceResult.Message, Severity.Error);
            }
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
