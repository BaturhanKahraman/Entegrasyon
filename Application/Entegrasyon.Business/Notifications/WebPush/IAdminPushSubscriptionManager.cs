using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Notifications.WebPush;

public interface IAdminPushSubscriptionManager
{
    Task<IResult> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth, string? userAgent);
    Task<IResult> UnsubscribeAsync(string endpoint);
    Task<List<AdminPushSubscription>> GetByUserAsync(Guid userId);
}
