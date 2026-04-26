using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Notifications.WebPush;

public sealed class AdminPushSubscriptionManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IAdminPushSubscriptionManager
{
    public async Task<IResult> SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth, string? userAgent)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var existing = await db.AdminPushSubscriptions
            .AsTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Endpoint == endpoint);

        if (existing is not null)
        {
            existing.P256dhKey = p256dh;
            existing.AuthKey = auth;
            existing.UserAgent = userAgent;
            existing.LastSeenAt = DateTimeOffset.UtcNow;
        }
        else
        {
            db.AdminPushSubscriptions.Add(new AdminPushSubscription
            {
                UserId = userId,
                Endpoint = endpoint,
                P256dhKey = p256dh,
                AuthKey = auth,
                UserAgent = userAgent,
                LastSeenAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return new SuccessResult("Abonelik kaydedildi.");
    }

    public async Task<IResult> UnsubscribeAsync(string endpoint)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var sub = await db.AdminPushSubscriptions
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Endpoint == endpoint);
        if (sub is null) return new ErrorResult("Abonelik bulunamadı.");
        db.AdminPushSubscriptions.Remove(sub);
        await db.SaveChangesAsync();
        return new SuccessResult("Abonelik kaldırıldı.");
    }

    public async Task<List<AdminPushSubscription>> GetByUserAsync(Guid userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.AdminPushSubscriptions
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }
}
