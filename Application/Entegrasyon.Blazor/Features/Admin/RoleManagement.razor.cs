using Entegrasyon.ApplicationBootstrap.Security;
using Entegrasyon.Blazor.Models;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Admin;

public partial class RoleManagement
{
    [Inject] private IRoleService RoleService { get; set; } = null!;
    [Inject] private IntegrationDbContext DbContext { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<Role> _roles = [];
    private List<PermissionModel> _allPermissions = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;
        try
        {
            _roles = await DbContext.Roles.Include(r => r.RoleClaims).ToListAsync();

            var allPermissions = AppPermissions.GetAllPermissions();
            _allPermissions = allPermissions
                .Select(p => new PermissionModel(p, FormatPermissionName(p)))
                .ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Veri yüklenirken hata oluştu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private static string FormatPermissionName(string permission)
    {
        var parts = permission.Split('.');
        if (parts.Length >= 3)
        {
            var action = parts[2];
            var actionName = action switch
            {
                "View" => "Görüntüle",
                "Create" => "Oluştur",
                "Edit" => "Düzenle",
                "Delete" => "Sil",
                _ => action
            };

            return $"{parts[1]} - {actionName}";
        }
        return permission;
    }

    private async Task OpenAddRoleDialog()
    {
        var parameters = new DialogParameters
        {
            { "IsEdit", false },
            { "AllPermissions", _allPermissions }
        };

        var dialog = await DialogService.ShowAsync<RoleDialog>("Yeni Rol", parameters);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is RoleDialog.RoleDto dto)
        {
            var roleDto = new AddRoleDto(dto.Name, dto.PermissionNames);

            var serviceResult = await RoleService.AddRole(roleDto);
            if (serviceResult.Success)
            {
                Snackbar.Add(serviceResult.Message, Severity.Success);
                await LoadData();
            }
            else
            {
                Snackbar.Add(serviceResult.Message, Severity.Error);
            }
        }
    }

    private async Task EditRole(Role role)
    {
        var parameters = new DialogParameters
        {
            { "IsEdit", true },
            { "AllPermissions", _allPermissions },
            { "Model", new RoleDialog.RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    PermissionNames = role.RoleClaims?.Select(c => c.Permission!).ToList() ?? []
                }
            }
        };

        var dialog = await DialogService.ShowAsync<RoleDialog>("Rol Düzenle", parameters);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is RoleDialog.RoleDto dto)
        {
            var editDto = new EditRoleDto(dto.Id, dto.Name, dto.PermissionNames);

            var serviceResult = await RoleService.UpdateRole(editDto);
            if (serviceResult.Success)
            {
                Snackbar.Add(serviceResult.Message, Severity.Success);
                await LoadData();
            }
            else
            {
                Snackbar.Add(serviceResult.Message, Severity.Error);
            }
        }
    }

    private async Task DeleteRole(Role role)
    {
        var result = await DialogService.ShowMessageBox(
            "Rol Sil",
            $"{role.Name} rolünü silmek istediğinizden emin misiniz?",
            yesText: "Sil",
            cancelText: "İptal");

        if (result == true)
        {
            var serviceResult = await RoleService.DeleteRole(role.Id);
            if (serviceResult.Success)
            {
                Snackbar.Add(serviceResult.Message, Severity.Success);
                await LoadData();
            }
            else
            {
                Snackbar.Add(serviceResult.Message, Severity.Error);
            }
        }
    }
}
