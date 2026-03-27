using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// İç ürün verisini Çiçeksepeti JSON formatına dönüştürür.
/// Her varyant için ayrı bir CiceksepetiProductRequest oluşturur.
/// </summary>
public sealed class CiceksepetiProductMapper(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<CiceksepetiProductMapper> logger) : ICiceksepetiProductMapper
{
    public async Task<IDataResult<CiceksepetiCreateProductsRequest>> MapToCreateRequestAsync(
        Guid productId, CancellationToken ct = default)
    {
        return await MapInternalAsync(productId, isActive: false, ct);
    }

    public async Task<IDataResult<CiceksepetiCreateProductsRequest>> MapToUpdateRequestAsync(
        Guid productId, CancellationToken ct = default)
    {
        return await MapInternalAsync(productId, isActive: true, ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<IDataResult<CiceksepetiCreateProductsRequest>> MapInternalAsync(
        Guid productId, bool isActive, CancellationToken ct)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.AttributeKeyValues)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null)
            return new ErrorDataResult<CiceksepetiCreateProductsRequest>(null!, "Ürün bulunamadı.");

        if (!product.ProductVariants.Any())
            return new ErrorDataResult<CiceksepetiCreateProductsRequest>(null!, "Ürünün varyantı yok.");

        // Kategori eşleştirmesi
        var categoryMatch = await dbContext.CategoryMarketplaces
            .FirstOrDefaultAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == CiceksepetiMarketPlaceId, ct);

        if (categoryMatch is null)
            return new ErrorDataResult<CiceksepetiCreateProductsRequest>(null!, "Kategori Çiçeksepeti'ye eşleştirilmemiş.");

        // Özellik eşleştirmeleri
        var attrMatches = await dbContext.CategoryAttributeMarketPlaceMatches
            .Where(m => m.MarketPlaceId == CiceksepetiMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeId, ct);

        // Özellik değer eşleştirmeleri
        var valueMatches = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .Where(m => m.MarketPlaceId == CiceksepetiMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeValueId, m => m.MarketPlaceCategoryAttributeValueId, ct);

        var productRequests = new List<CiceksepetiProductRequest>();
        var mainProductCode = product.StockCode ?? product.Id.ToString("N")[..16];

        foreach (var variant in product.ProductVariants)
        {
            var stockCode = variant.Barcode ?? $"{product.Id:N}-{variant.Id:N}";
            var stock = variant.BranchOfficeStocks.Sum(s => s.CurrentStock);
            var images = variant.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => fileStorage.GetPublicUrl(i.StorageKey ?? ""))
                .Where(url => !string.IsNullOrEmpty(url))
                .Take(10)
                .ToList();

            // Özellik mapping: product-level attributes
            var attributes = new List<CiceksepetiAttributeRequest>();
            foreach (var akv in product.AttributeKeyValues)
            {
                if (!attrMatches.TryGetValue(akv.CategoryAttributeId, out var marketplaceAttrId))
                    continue;

                int valueId = 0;
                if (akv.AttributeValueId.HasValue && akv.AttributeValueId.Value > 0 &&
                    valueMatches.TryGetValue(akv.AttributeValueId.Value, out var marketplaceValueId))
                {
                    valueId = marketplaceValueId;
                }

                // TextLength: use 0 unless custom value has a length
                var textLength = 0;
                if (!string.IsNullOrWhiteSpace(akv.CustomValue))
                    textLength = akv.CustomValue.Length;

                attributes.Add(new CiceksepetiAttributeRequest(
                    Id: marketplaceAttrId,
                    ValueId: valueId,
                    TextLength: textLength));
            }

            productRequests.Add(new CiceksepetiProductRequest(
                ProductName: product.Title,
                ProductCode: product.StockCode,
                CategoryId: categoryMatch.MarketPlaceCategoryId,
                StockCode: stockCode,
                MainProductCode: mainProductCode,
                Description: product.Description,
                SalesPrice: variant.SalePrice,
                ListPrice: variant.ListPrice,
                StockQuantity: stock,
                Barcode: variant.Barcode,
                Images: images.Count > 0 ? images : null,
                Attributes: attributes.Count > 0 ? attributes : null));
        }

        logger.LogInformation(
            "Çiçeksepeti product mapped: {ProductId}, {ItemCount} variants, isActive={IsActive}",
            productId, productRequests.Count, isActive);

        return new SuccessDataResult<CiceksepetiCreateProductsRequest>(
            new CiceksepetiCreateProductsRequest(productRequests));
    }
}
