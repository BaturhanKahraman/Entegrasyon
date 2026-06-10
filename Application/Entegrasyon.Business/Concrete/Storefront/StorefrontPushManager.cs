using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontPushManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontPushManager
{
    public async Task<IResult> SubscribeAsync(int tenantId, int? customerId, string endpoint, string p256dh, string auth)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Check for duplicate endpoint
        var existing = await dbContext.StorefrontPushSubscriptions
            .FirstOrDefaultAsync(x => x.Endpoint == endpoint);

        if (existing is not null)
        {
            // Update keys (they may have changed)
            existing.P256dhKey = p256dh;
            existing.AuthKey = auth;
            existing.CustomerId = customerId ?? existing.CustomerId;

            dbContext.StorefrontPushSubscriptions.Update(existing);
            await dbContext.SaveChangesAsync();

            return new SuccessResult("Abonelik guncellendi.");
        }

        var subscription = new StorefrontPushSubscription
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Endpoint = endpoint,
            P256dhKey = p256dh,
            AuthKey = auth
        };

        dbContext.StorefrontPushSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Push bildirim aboneligi kaydedildi.");
    }

    public async Task<IResult> UnsubscribeAsync(string endpoint)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var subscription = await dbContext.StorefrontPushSubscriptions
            .FirstOrDefaultAsync(x => x.Endpoint == endpoint);

        if (subscription is null)
            return new ErrorResult("Abonelik bulunamadı.");

        dbContext.StorefrontPushSubscriptions.Remove(subscription);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Push bildirim aboneligi kaldirildi.");
    }

    public async Task<IDataResult<int>> GetSubscriberCountAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var count = await dbContext.StorefrontPushSubscriptions
            .CountAsync(x => x.TenantId == tenantId);

        return new SuccessDataResult<int>(count);
    }
}
