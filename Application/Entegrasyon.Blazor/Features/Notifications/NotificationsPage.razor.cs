using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

public partial class NotificationsPage : ComponentBase
{
    [Inject] private INotificationManager NotificationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private Guid _userId;
    private List<Notification> _notifications = [];
    private bool _onlyUnread = false;
    private NotificationSeverity? _severityFilter;
    private NotificationCategory? _categoryFilter;

    private IEnumerable<Notification> FilteredNotifications => _notifications
        .Where(n => _severityFilter == null || n.Severity == _severityFilter)
        .Where(n => _categoryFilter == null || n.Category == _categoryFilter);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        await LoadNotifications();
    }

    private async Task LoadNotifications()
    {
        var result = await NotificationManager.GetNotificationsForUser(_userId, _onlyUnread);
        _notifications = result.ToList();
    }

    private async Task OnRowClick(Notification notification)
    {
        if (!notification.IsRead)
            await NotificationManager.MarkAsRead(notification.Id, _userId);

        notification.IsRead = true;

        if (notification.ActionUrl is not null)
            NavigationManager.NavigateTo(notification.ActionUrl);
    }

    private async Task MarkAllAsRead()
    {
        await NotificationManager.MarkAllAsRead(_userId);
        foreach (var n in _notifications)
            n.IsRead = true;
        StateHasChanged();
    }

    private async Task OnOnlyUnreadChanged(bool value)
    {
        _onlyUnread = value;
        await LoadNotifications();
    }

    private async Task DismissNotification(Notification notification)
    {
        await NotificationManager.DismissNotification(notification.Id, _userId);
        _notifications.Remove(notification);
        StateHasChanged();
    }

    private async Task DismissAllRead()
    {
        await NotificationManager.DismissAllRead(_userId);
        _notifications.RemoveAll(n => n.IsRead);
        StateHasChanged();
    }
}
