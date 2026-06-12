using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class MarketplaceOverrideManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    IProductSyncManager syncManager,
    ILogger<MarketplaceOverrideManager> logger) : IMarketplaceOverrideManager
{
    public async Task<IDataResult<MarketplaceOverrideDetailDto>> GetOverridesAsync(Guid productId, int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorDataResult<MarketplaceOverrideDetailDto>(null!, "Ürün bulunamadı.");

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == marketPlaceId);

        if (marketplace is null)
            return new ErrorDataResult<MarketplaceOverrideDetailDto>(null!, "Pazaryeri bulunamadı.");

        var pm = await dbContext.ProductMarketplaces
            .AsNoTracking()
            .Include(x => x.VariantOverrides)
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == marketPlaceId);

        var variantOverrides = pm?.VariantOverrides?.ToList() ?? [];

        var dto = new MarketplaceOverrideDetailDto
        {
            MarketPlaceId = marketPlaceId,
            MarketPlaceName = marketplace.Name,
            TitleOverride = pm?.TitleOverride,
            DescriptionOverride = pm?.DescriptionOverride,
            VariantOverrides = product.ProductVariants.Select(v =>
            {
                var vo = variantOverrides.FirstOrDefault(o => o.ProductVariantId == v.Id);
                var label = v.ProductVariantAttributes is { Count: > 0 }
                    ? string.Join(" / ", v.ProductVariantAttributes
                        .Where(a => a.IsVarianter || a.IsSlicer)
                        .Select(a => a.CategoryAttributeValue ?? "?"))
                    : v.Barcode;

                return new VariantPriceOverrideDetailDto
                {
                    ProductVariantId = v.Id,
                    VariantLabel = label!,
                    Barcode = v.Barcode!,
                    OriginalListPrice = v.ListPrice,
                    OriginalSalePrice = v.SalePrice,
                    ListPriceOverride = vo?.ListPriceOverride,
                    SalePriceOverride = vo?.SalePriceOverride
                };
            }).ToList()
        };

        return new SuccessDataResult<MarketplaceOverrideDetailDto>(dto);
    }

    public async Task<IResult> SaveOverridesAsync(SaveMarketplaceOverridesDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // 1. Validation
        await validator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        var product = await dbContext.MainProducts.AsNoTracking()
            .AnyAsync(p => p.Id == dto.ProductId);
        if (!product)
            return new ErrorResult("Ürün bulunamadı.");

        var marketplaceExists = await dbContext.MarketPlaces.AsNoTracking()
            .AnyAsync(m => m.Id == dto.MarketPlaceId);
        if (!marketplaceExists)
            return new ErrorResult("Pazaryeri bulunamadı.");

        // 3. Execution
        var pm = await dbContext.ProductMarketplaces
            .Include(x => x.VariantOverrides)
            .FirstOrDefaultAsync(x => x.ProductId == dto.ProductId && x.MarketPlaceId == dto.MarketPlaceId);

        if (pm is null)
        {
            pm = new ProductMarketplace
            {
                ProductId = dto.ProductId,
                MarketPlaceId = dto.MarketPlaceId,
                Status = MarketplaceProductStatus.Pending,
                TitleOverride = dto.TitleOverride,
                DescriptionOverride = dto.DescriptionOverride
            };
            dbContext.ProductMarketplaces.Add(pm);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            pm.TitleOverride = dto.TitleOverride;
            pm.DescriptionOverride = dto.DescriptionOverride;
            dbContext.ProductMarketplaces.Update(pm);
        }

        // Replace variant overrides
        if (pm.VariantOverrides.Count > 0)
            dbContext.ProductVariantMarketplaceOverrides.RemoveRange(pm.VariantOverrides);

        var newOverrides = dto.VariantOverrides
            .Where(vo => vo.ListPriceOverride.HasValue || vo.SalePriceOverride.HasValue)
            .Select(vo => new ProductVariantMarketplaceOverride
            {
                ProductMarketplaceId = pm.Id,
                ProductVariantId = vo.ProductVariantId,
                ListPriceOverride = vo.ListPriceOverride,
                SalePriceOverride = vo.SalePriceOverride
            })
            .ToList();

        if (newOverrides.Count > 0)
            dbContext.ProductVariantMarketplaceOverrides.AddRange(newOverrides);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Marketplace overrides saved for Product {ProductId}, MarketPlace {MarketPlaceId}",
            dto.ProductId, dto.MarketPlaceId);

        return new SuccessResult("Pazaryeri override'ları kaydedildi.");
    }

    public async Task<IResult> SaveOverridesAndPublishAsync(SaveMarketplaceOverridesDto dto)
    {
        var saveResult = await SaveOverridesAsync(dto);
        if (!saveResult.Success)
            return saveResult;

        var syncResult = await syncManager.SyncProductAsync(dto.ProductId, dto.MarketPlaceId);
        if (!syncResult.Success)
            return new ErrorResult($"Override'lar kaydedildi ancak senkronizasyon başlatılamadı: {syncResult.Message}");

        return new SuccessResult("Override'lar kaydedildi ve senkronizasyon başlatıldı.");
    }
}
