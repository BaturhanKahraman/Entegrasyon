using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product.Discount;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class DiscountManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    IProductSyncManager syncManager,
    IApplicationLogManager applicationLogManager,
    ILogger<DiscountManager> logger) : IDiscountManager
{
    public async Task<IDataResult<DiscountPreviewDto>> GetDiscountPreviewAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.Id,
                p.Title,
                Variants = p.ProductVariants.Select(v => new
                {
                    v.Id,
                    v.Barcode,
                    v.CostPrice,
                    v.ListPrice,
                    v.SalePrice,
                    v.ECommercePrice,
                    v.VatRate,
                    Attributes = v.ProductVariantAttributes
                        .Where(a => a.IsVarianter && !string.IsNullOrEmpty(a.CategoryAttributeValue))
                        .Select(a => a.CategoryAttributeValue)
                        .ToList()
                }).ToList(),
                MarketplaceRecords = p.ProductMarketplaces.Select(pm => new
                {
                    pm.MarketPlaceId,
                    pm.MarketPlace.Name,
                    pm.ProductId
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorDataResult<DiscountPreviewDto>(null!, "Ürün bulunamadı.");

        var variants = product.Variants.Select(v =>
        {
            var label = v.Attributes.Count != 0
                ? string.Join(" / ", v.Attributes)
                : v.Barcode;
            var profitMargin = v.SalePrice - v.CostPrice;
            var profitMarginPercent = v.CostPrice > 0
                ? (profitMargin / v.CostPrice) * 100
                : 0;

            return new VariantDiscountInfoDto(
                v.Id, v.Barcode!, label!,
                v.CostPrice, v.ListPrice, v.SalePrice, v.ECommercePrice, v.VatRate,
                profitMargin, Math.Round(profitMarginPercent, 2));
        }).ToList();

        // Mock marketplace fiyatları — gerçek API entegrasyonu henüz yok
        var marketplacePrices = new List<MarketplacePriceInfoDto>();
        foreach (var mp in product.MarketplaceRecords)
        {
            foreach (var v in product.Variants)
            {
                marketplacePrices.Add(new MarketplacePriceInfoDto(
                    mp.MarketPlaceId, mp.Name,
                    v.Id, v.Barcode!,
                    v.ListPrice,
                    Math.Round(v.SalePrice * 1.05m, 2)));
            }
        }

        var preview = new DiscountPreviewDto(product.Id, product.Title, variants, marketplacePrices);
        return new SuccessDataResult<DiscountPreviewDto>(preview);
    }

    public async Task<IDataResult<DiscountResultDto>> ApplyDiscountAsync(ApplyDiscountDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // 1. Validation
        await validator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (product is null)
            return new ErrorDataResult<DiscountResultDto>(null!, "Ürün bulunamadı.");

        var belowCostVariants = product.ProductVariants
            .Where(v =>
            {
                var newSalePrice = v.SalePrice * (1 - dto.DiscountPercentage / 100);
                return newSalePrice < v.CostPrice;
            })
            .Select(v => v.Barcode)
            .ToList();

        if (belowCostVariants.Count > 0)
        {
            logger.LogWarning(
                "İndirim sonrası maliyet altına düşen varyantlar: {Barcodes}",
                string.Join(", ", belowCostVariants));
        }

        // 3. Execution
        foreach (var variant in product.ProductVariants)
        {
            variant.SalePrice = Math.Round(variant.SalePrice * (1 - dto.DiscountPercentage / 100), 2);
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"'{product.Title}' ürününe %{dto.DiscountPercentage} indirim uygulandı.",
            LogType.Product, LogAction.Update);

        // Seçili pazaryerlerine sıralı sync
        var syncedMarketplaces = new List<string>();
        foreach (var marketPlaceId in dto.TargetMarketPlaceIds)
        {
            var syncResult = await syncManager.SyncProductAsync(dto.ProductId, marketPlaceId);
            if (syncResult.Success)
            {
                var mp = await dbContext.MarketPlaces.FindAsync(marketPlaceId);
                syncedMarketplaces.Add(mp?.Name ?? $"Marketplace #{marketPlaceId}");
            }
        }

        var result = new DiscountResultDto(product.ProductVariants.Count, syncedMarketplaces);
        return new SuccessDataResult<DiscountResultDto>(result, "İndirim başarıyla uygulandı.");
    }
}
