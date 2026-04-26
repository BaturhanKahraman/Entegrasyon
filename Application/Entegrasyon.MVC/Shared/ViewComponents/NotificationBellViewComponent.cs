using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Shared.ViewComponents;

public sealed record NotificationBellViewModel(
    int UnreadCount,
    IReadOnlyList<Notification> Notifications);

public class NotificationBellViewComponent(IServiceScopeFactory scopeFactory) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Use own scope to avoid DbContext conflicts in layout rendering
        using var scope = scopeFactory.CreateScope();
        var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null || !Guid.TryParse(userId, out var uid))
        {
            return View(new NotificationBellViewModel(0, []));
        }

        var notifications = (await notificationManager.GetNotificationsForUser(uid, onlyUnread: true, take: 10)).ToList();
        return View(new NotificationBellViewModel(notifications.Count, notifications));
    }
}
