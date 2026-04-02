using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Shared.ViewComponents;

public class NotificationBellViewComponent(IServiceScopeFactory scopeFactory) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Use own scope to avoid DbContext conflicts in layout rendering
        using var scope = scopeFactory.CreateScope();
        var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int unreadCount = 0;

        if (userId is not null && Guid.TryParse(userId, out var uid))
        {
            var notifications = await notificationManager.GetNotificationsForUser(uid, onlyUnread: true);
            unreadCount = notifications.Count();
        }

        return View(unreadCount);
    }
}
