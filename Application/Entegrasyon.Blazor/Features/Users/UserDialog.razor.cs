using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Users;

public partial class UserDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public UserViewModel Model { get; set; } = new();
    [Parameter] public bool IsEdit { get; set; }
    [Parameter] public List<Role> Roles { get; set; } = [];

    public string NewUserPassword { get; set; } = string.Empty;
    public IEnumerable<int> SelectedRoleIds { get; set; } = new HashSet<int>();

    private MudForm _form = null!;
    private bool _success;

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        _form.Validate();
        if (!_success) return;

        MudDialog.Close(DialogResult.Ok(new UserDialogResult
        {
            Model = Model,
            Password = NewUserPassword,
            RoleIds = SelectedRoleIds.ToList()
        }));
    }

    public class UserViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
    }

    public class UserDialogResult
    {
        public UserViewModel Model { get; set; } = new();
        public string Password { get; set; } = string.Empty;
        public List<int> RoleIds { get; set; } = [];
    }
}
