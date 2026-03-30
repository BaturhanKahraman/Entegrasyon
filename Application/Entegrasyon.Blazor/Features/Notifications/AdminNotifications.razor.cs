using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

[Authorize(Policy = "Permissions.Notifications.Manage")]
public partial class AdminNotifications : ComponentBase
{
    [Inject] private INotificationManager NotificationManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<Notification> _notifications = [];
    private bool _loading = true;
    private string _searchText = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadNotifications();
    }

    private async Task LoadNotifications()
    {
        _loading = true;
        _notifications = await NotificationManager.GetAllNotificationsAsync();
        _loading = false;
    }

    private async Task OpenSendDialog()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<SendNotificationDialog>("Yeni Bildirim Gonder", options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadNotifications();
        }
    }
}
