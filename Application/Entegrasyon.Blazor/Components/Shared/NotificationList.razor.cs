using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class NotificationList : ComponentBase, IDisposable
{
    [Inject] private EventChannel<NotificationEvent> NotificationEventChannel { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NotificationManager NotificationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private List<Notification> _notifications = new();
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadNotifications();
        await ListenForNotifications();
    }

    private async Task LoadNotifications()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // For now, use a mock user ID since we don't have user management fully set up
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Mock user ID
            _notifications = (await NotificationManager.GetNotificationsForUser(userId, onlyUnread: true)).ToList();
        }
    }

    private async Task ListenForNotifications()
    {
        await foreach (var notificationEvent in NotificationEventChannel.Reader.ReadAllAsync(_cts.Token))
        {
            // Check if this notification is for current user
            // For now, show all as snackbar
            Snackbar.Add(notificationEvent.Content, Severity.Info, config =>
            {
                config.ShowCloseIcon = true;
                config.RequireInteraction = true;
            });

            // Add to list
            var notification = new Notification
            {
                Id = notificationEvent.NotificationId,
                Header = notificationEvent.Header,
                Content = notificationEvent.Content,
                CreatedAt = notificationEvent.OccurredAt,
                IsRead = false
            };
            _notifications.Insert(0, notification);
            StateHasChanged();
        }
    }

    private async Task MarkAsRead(long notificationId)
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Mock user ID
            await NotificationManager.MarkAsRead(notificationId, userId);

            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTimeOffset.UtcNow;
                StateHasChanged();
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}