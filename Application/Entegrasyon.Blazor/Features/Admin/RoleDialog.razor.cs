using Entegrasyon.Blazor.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Admin;

public partial class RoleDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public RoleDto Model { get; set; } = new();
    [Parameter] public bool IsEdit { get; set; }
    [Parameter] public List<PermissionModel> AllPermissions { get; set; } = [];

    public HashSet<string> SelectedPermissions { get; set; } = [];

    private MudForm _form = null!;
    private bool _success;

    protected override void OnParametersSet()
    {
        if (Model.PermissionNames != null)
        {
            SelectedPermissions = new HashSet<string>(Model.PermissionNames);
        }
    }

    private void OnPermissionChanged(string permission, bool isChecked)
    {
        if (isChecked)
            SelectedPermissions.Add(permission);
        else
            SelectedPermissions.Remove(permission);
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        _form.Validate();
        if (!_success) return;

        Model.PermissionNames = SelectedPermissions.ToList();
        MudDialog.Close(DialogResult.Ok(Model));
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> PermissionNames { get; set; } = [];
    }
}
