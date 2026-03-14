using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

public partial class NotificationBell : ComponentBase, IDisposable
{
    [Inject] private INotificationDeliveryService DeliveryService { get; set; } = null!;
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private Guid _userId;
    private List<Notification> _recentNotifications = [];
    private int _unreadCount;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        await using var scope = ScopeFactory.CreateAsyncScope();
        var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();
        var all = await notificationManager.GetNotificationsForUser(_userId, onlyUnread: false);
        _recentNotifications = all.Take(10).ToList();
        _unreadCount = _recentNotifications.Count(n => !n.IsRead);

        DeliveryService.Subscribe(_userId, HandleNotification);
        StateHasChanged();
    }

    // ÖNEMLI: Member method referansı — lambda kullanılmaz (Unsubscribe çalışmaz)
    private async Task HandleNotification(NotificationEvent evt)
    {
        var notification = new Notification
        {
            Id = evt.NotificationId,
            Header = evt.Header,
            Content = evt.Content,
            Severity = evt.Severity,
            Category = evt.Category,
            ActionUrl = evt.ActionUrl,
            CreatedAt = evt.OccurredAt,
            IsRead = false
        };

        // Tüm state mutation InvokeAsync içinde — Blazor dispatch queue thread-safe
        await InvokeAsync(() =>
        {
            _recentNotifications.Insert(0, notification);
            if (_recentNotifications.Count > 10)
                _recentNotifications.RemoveAt(10);
            _unreadCount++;

            var severity = evt.Severity switch
            {
                NotificationSeverity.Success => Severity.Success,
                NotificationSeverity.Warning => Severity.Warning,
                NotificationSeverity.Error   => Severity.Error,
                _                            => Severity.Info
            };

            Snackbar.Add(evt.Header, severity);
            StateHasChanged();
        });
    }

    private async Task MarkAsRead(long notificationId)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();
        await notificationManager.MarkAsRead(notificationId, _userId);
        var n = _recentNotifications.FirstOrDefault(x => x.Id == notificationId);
        if (n is not null)
        {
            n.IsRead = true;
            _unreadCount = Math.Max(0, _unreadCount - 1);
        }
    }

    public void Dispose()
        => DeliveryService.Unsubscribe(_userId, HandleNotification);
}
