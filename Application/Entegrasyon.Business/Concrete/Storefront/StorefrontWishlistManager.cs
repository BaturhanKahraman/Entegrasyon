using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontWishlistManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontWishlistManager
{
    public async Task<IDataResult<List<StorefrontWishlistItem>>> GetWishlistAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var items = await dbContext.StorefrontWishlistItems
            .AsNoTracking()
            .Include(w => w.Product)
            .Where(w => w.TenantId == tenantId && w.CustomerId == customerId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontWishlistItem>>(items);
    }

    public async Task<IResult> AddToWishlistAsync(int tenantId, int customerId, Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var exists = await dbContext.StorefrontWishlistItems
            .AnyAsync(w => w.TenantId == tenantId && w.CustomerId == customerId && w.ProductId == productId);

        if (exists)
            return new ErrorResult("Bu urun zaten favorilerinizde.");

        var item = new StorefrontWishlistItem
        {
            TenantId = tenantId,
            CustomerId = customerId,
            ProductId = productId,
            AddedAt = DateTimeOffset.UtcNow
        };

        dbContext.StorefrontWishlistItems.Add(item);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Urun favorilere eklendi.");
    }

    public async Task<IResult> RemoveFromWishlistAsync(int tenantId, int customerId, Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var item = await dbContext.StorefrontWishlistItems
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.CustomerId == customerId && w.ProductId == productId);

        if (item is null)
            return new ErrorResult("Urun favorilerde bulunamadi.");

        dbContext.StorefrontWishlistItems.Remove(item);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Urun favorilerden cikarildi.");
    }

    public async Task<bool> IsInWishlistAsync(int tenantId, int customerId, Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        return await dbContext.StorefrontWishlistItems
            .AnyAsync(w => w.TenantId == tenantId && w.CustomerId == customerId && w.ProductId == productId);
    }
}
