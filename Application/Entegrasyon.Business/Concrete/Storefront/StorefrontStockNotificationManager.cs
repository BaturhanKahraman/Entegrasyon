using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontStockNotificationManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontStockNotificationManager
{
    public async Task<IResult> SubscribeAsync(int tenantId, Guid productVariantId, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new ErrorResult("E-posta alanızorunludur.");

        if (productVariantId == Guid.Empty)
            return new ErrorResult("Urun varyanti belirtilmelidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.StorefrontStockNotifications
            .FirstOrDefaultAsync(n =>
                n.TenantId == tenantId &&
                n.ProductVariantId == productVariantId &&
                n.Email == email);

        if (existing is not null)
        {
            if (!existing.IsNotified)
                return new ErrorResult("Bu urun icin zaten bildirim kaydınız bulunmaktadir.");

            // Re-subscribe after previous notification
            existing.IsNotified = false;
            existing.NotifiedAt = null;
            dbContext.StorefrontStockNotifications.Update(existing);
            await dbContext.SaveChangesAsync();
            return new SuccessResult("Stok bildirimi yeniden aktif edildi.");
        }

        var notification = new StorefrontStockNotification
        {
            TenantId = tenantId,
            ProductVariantId = productVariantId,
            Email = email,
            IsNotified = false
        };

        dbContext.StorefrontStockNotifications.Add(notification);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Stok bildirimi basariyla olusturuldu. Urun stoga girdiginde bilgilendirileceksiniz.");
    }

    public async Task<IDataResult<int>> GetSubscriberCountAsync(int tenantId, Guid productVariantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var count = await dbContext.StorefrontStockNotifications
            .AsNoTracking()
            .CountAsync(n =>
                n.TenantId == tenantId &&
                n.ProductVariantId == productVariantId &&
                !n.IsNotified);

        return new SuccessDataResult<int>(count);
    }
}
