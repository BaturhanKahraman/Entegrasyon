using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Product + Variants -> List&lt;TrendyolProductItem&gt; donusumu.
/// Her varyant ayri bir TrendyolProductItem olarak olusturulur -- Trendyol V2 API varyant bazli calisir.
/// Ayni urunun tum varyantlari ayni productMainId altinda gruplanir.
/// </summary>
public sealed class TrendyolProductMapper(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<TrendyolProductMapper> logger) : ITrendyolProductMapper
{
    public async Task<IDataResult<TrendyolCreateProductRequest>> MapProductAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Urunu tum iliskileriyle cek
        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks).ThenInclude(s => s.BranchOffice)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.AttributeKeyValues)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Urun bulunamadı.");

        if (product.ProductVariants.Count == 0)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Urunun varyanti yok.");

        // Override'lari yukle (varsa)
        var marketplace = await dbContext.ProductMarketplaces
            .AsNoTracking()
            .Include(pm => pm.VariantOverrides)
            .FirstOrDefaultAsync(pm => pm.ProductId == productId
                && pm.MarketPlaceId == TrendyolMarketPlaceId && !pm.IsDeleted);

        var effectiveTitle = marketplace?.TitleOverride ?? product.Title;
        var effectiveDescription = marketplace?.DescriptionOverride ?? product.Description;

        // Marketplace eslestirmeleri -- brand, category
        var brandMatch = product.BrandId.HasValue
            ? await dbContext.BrandMarketPlaceMatches.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ApplicationBrandId == product.BrandId && m.MarketPlaceId == TrendyolMarketPlaceId)
            : null;

        if (brandMatch is null)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Marka Trendyol eslestirmesi bulunamadı.");

        var categoryMatch = await dbContext.CategoryMarketPlaceMatches.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ApplicationCategoryId == product.CategoryId && m.MarketPlaceId == TrendyolMarketPlaceId);

        if (categoryMatch is null)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Kategori Trendyol eslestirmesi bulunamadı.");

        // Attribute eslestirmelerini toplu cek
        var attributeIds = product.AttributeKeyValues.Select(a => a.CategoryAttributeId).Distinct().ToList();
        var attributeMatches = await dbContext.CategoryAttributeMarketPlaceMatches.AsNoTracking()
            .Where(m => attributeIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == TrendyolMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeId);

        var valueIds = product.AttributeKeyValues
            .Select(a => a.AttributeValueId)
            .Distinct().ToList();
        var valueMatches = await dbContext.CategoryAttributeValueMarketPlaceMatches.AsNoTracking()
            .Where(m => valueIds.Contains(m.ApplicationCategoryAttributeValueId) && m.MarketPlaceId == TrendyolMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeValueId, m => m.MarketPlaceCategoryAttributeValueId);
        var valueNames = await dbContext.CategoryAttributeValues.AsNoTracking()
            .Where(v => valueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name);

        // Varyant seviyesi attribute degerlerinin CategoryAttributeId'lerini toplu cek (N+1 onleme:
        // aksi halde her varyant×ozellik icin ayri sorgu atilirdi).
        var pvaValueIds = product.ProductVariants
            .Where(v => v.ProductVariantAttributes is not null)
            .SelectMany(v => v.ProductVariantAttributes)
            .Where(p => p.CategoryAttributeValueId.HasValue)
            .Select(p => p.CategoryAttributeValueId!.Value)
            .Distinct().ToList();
        var pvaValueCategoryIds = pvaValueIds.Count == 0
            ? new Dictionary<int, int>()
            : await dbContext.CategoryAttributeValues.AsNoTracking()
                .Where(v => pvaValueIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.CategoryAttributeId);

        // Marketplace'e stok gonderecek depo ID'lerini belirle
        var warehouseIds = await dbContext.MarketPlaceWarehouses.AsNoTracking()
            .Where(w => w.MarketPlaceId == TrendyolMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        // Fallback: MarketPlaceWarehouse kaydi yoksa IsDefaultMarketPlaceStock olan depolari kullan
        if (warehouseIds.Count == 0)
        {
            warehouseIds = await dbContext.BranchOffices.AsNoTracking()
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .ToListAsync();
        }

        var productMainId = product.Id.ToString();
        var items = new List<TrendyolProductItem>();

        foreach (var variant in product.ProductVariants)
        {
            // Quantity: secili depolarin stok toplami
            var quantity = variant.BranchOfficeStocks
                .Where(s => warehouseIds.Contains(s.BranchOfficeId))
                .Sum(s => s.CurrentStock);

            // Gorseller: MinIO StorageKey -> public URL
            var images = variant.Images
                .Where(i => !string.IsNullOrEmpty(i.StorageKey))
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new TrendyolProductImage(fileStorage.GetPublicUrl(i.StorageKey!)))
                .ToList();

            if (images.Count == 0)
            {
                logger.LogWarning("Varyant {Barcode} gorseli yok, atlaniyor", variant.Barcode);
                continue;
            }

            // Override varsa override fiyatlarini kullan, yoksa orijinal
            var variantOverride = marketplace?.VariantOverrides
                ?.FirstOrDefault(vo => vo.ProductVariantId == variant.Id);
            var effectiveListPrice = variantOverride?.ListPriceOverride ?? variant.ListPrice;
            var effectiveSalePrice = variantOverride?.SalePriceOverride ?? variant.SalePrice;

            // SalePrice <= ListPrice kontrolu
            var salePrice = effectiveSalePrice > effectiveListPrice ? effectiveListPrice : effectiveSalePrice;

            // VatRate: 0, 1, 10, 20 olmali
            var vatRate = (int)variant.VatRate;
            if (vatRate is not (0 or 1 or 10 or 20))
            {
                logger.LogWarning("Varyant {Barcode} gecersiz VatRate={VatRate}, 20 olarak ayarlandi", variant.Barcode, vatRate);
                vatRate = 20;
            }

            // Attributes: urun seviyesi (AttributeKeyValues) + varyant seviyesi (ProductVariantAttributes)
            var attributes = new List<TrendyolProductAttribute>();

            // Urun seviyesi ozellikler
            foreach (var akv in product.AttributeKeyValues)
            {
                if (!attributeMatches.TryGetValue(akv.CategoryAttributeId, out var trendyolAttrId))
                    continue;

                int? trendyolValueId = null;
                string? customValue = null;

                if (valueMatches.TryGetValue(akv.AttributeValueId, out var mappedValueId))
                {
                    trendyolValueId = mappedValueId;
                }
                else if (valueNames.TryGetValue(akv.AttributeValueId, out var name) && !string.IsNullOrEmpty(name))
                {
                    customValue = name; // no marketplace match -> send the value's own name as custom string
                }
                else
                {
                    continue;
                }

                attributes.Add(new TrendyolProductAttribute(trendyolAttrId, trendyolValueId, customValue));
            }

            // Varyant seviyesi ozellikler (renk, beden vb.)
            if (variant.ProductVariantAttributes is not null)
            {
                foreach (var pva in variant.ProductVariantAttributes)
                {
                    if (!pva.CategoryAttributeValueId.HasValue) continue;

                    if (!pvaValueCategoryIds.TryGetValue(pva.CategoryAttributeValueId.Value, out var pvaCatAttrId)
                        || !attributeMatches.TryGetValue(pvaCatAttrId, out var tAttrId))
                        continue;

                    if (valueMatches.TryGetValue(pva.CategoryAttributeValueId.Value, out var trendyolValueId))
                    {
                        attributes.Add(new TrendyolProductAttribute(tAttrId, trendyolValueId, null));
                    }
                    else if (!string.IsNullOrEmpty(pva.CategoryAttributeValue))
                    {
                        attributes.Add(new TrendyolProductAttribute(tAttrId, null, pva.CategoryAttributeValue));
                    }
                }
            }

            var item = new TrendyolProductItem(
                Barcode: variant.Barcode!,
                Title: effectiveTitle.Length > 100 ? effectiveTitle[..100] : effectiveTitle,
                ProductMainId: productMainId,
                BrandId: brandMatch.MarketPlaceBrandId,
                CategoryId: categoryMatch.MarketPlaceCategoryId,
                ListPrice: effectiveListPrice,
                SalePrice: salePrice,
                VatRate: vatRate,
                StockCode: variant.Barcode!,
                DimensionalWeight: variant.DimensionalWeight,
                Description: effectiveDescription!.Length > 30000 ? effectiveDescription[..30000] : effectiveDescription,
                Quantity: quantity,
                Images: images,
                Attributes: attributes);

            items.Add(item);
        }

        if (items.Count == 0)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Hicbir varyant Trendyol'a gonderilemedi (gorsel eksik olabilir).");

        logger.LogInformation("Product {ProductId} mapped to {ItemCount} TrendyolProductItems", productId, items.Count);
        return new SuccessDataResult<TrendyolCreateProductRequest>(new TrendyolCreateProductRequest(items));
    }
}
