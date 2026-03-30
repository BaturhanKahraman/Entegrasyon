using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

public partial class SendNotificationDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private INotificationManager NotificationManager { get; set; } = null!;
    [Inject] private INotificationRecipientResolver RecipientResolver { get; set; } = null!;
    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private IRoleService RoleService { get; set; } = null!;
    [Inject] private IBranchOfficeManager BranchOfficeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private string _header = string.Empty;
    private string _content = string.Empty;
    private NotificationSeverity _severity = NotificationSeverity.Info;
    private NotificationCategory _category = NotificationCategory.Sistem;
    private string _recipientType = "all";
    private IEnumerable<Guid> _selectedUserIds = [];
    private int _selectedRoleId;
    private int _selectedBranchId;
    private bool _sending;
    private string? _errorMessage;

    private List<UserDetailListDto> _users = [];
    private List<Role> _roles = [];
    private List<Entity.BranchOffice> _branches = [];

    protected override async Task OnInitializedAsync()
    {
        var usersResult = await UserManager.GetPaginatedUserDetails(0, 1000);
        if (usersResult.Success)
            _users = usersResult.Data.Items.ToList();

        var rolesResult = await RoleService.GetRolesSelectList();
        _roles = rolesResult.ToList();

        var branchesResult = await BranchOfficeManager.GetBranchList();
        if (branchesResult.Success)
            _branches = branchesResult.Data ?? [];
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task Send()
    {
        _errorMessage = null;

        if (string.IsNullOrWhiteSpace(_header) || string.IsNullOrWhiteSpace(_content))
        {
            _errorMessage = "Baslik ve icerik zorunludur.";
            return;
        }

        _sending = true;
        try
        {
            var userIds = _recipientType switch
            {
                "users" => _selectedUserIds.ToList(),
                "role" => await RecipientResolver.ResolveByRoleAsync(_selectedRoleId),
                "branch" => await RecipientResolver.ResolveByBranchOfficeAsync(_selectedBranchId),
                "all" => await RecipientResolver.ResolveAllActiveUsersAsync(),
                _ => new List<Guid>()
            };

            if (userIds.Count == 0)
            {
                _errorMessage = "En az bir alici secilmelidir.";
                _sending = false;
                return;
            }

            await NotificationManager.SendNotification(_header, _content, _severity, _category, userIds);
            Snackbar.Add($"Bildirim {userIds.Count} kisiye gonderildi.", MudBlazor.Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            _errorMessage = $"Bildirim gonderilemedi: {ex.Message}";
        }
        finally
        {
            _sending = false;
        }
    }
}
