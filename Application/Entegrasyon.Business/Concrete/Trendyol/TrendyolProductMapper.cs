using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Product + Variants → List&lt;TrendyolProductItem&gt; dönüşümü.
/// Her varyant ayrı bir TrendyolProductItem olarak oluşturulur — Trendyol V2 API varyant bazlı çalışır.
/// Aynı ürünün tüm varyantları aynı productMainId altında gruplanır.
/// </summary>
public sealed class TrendyolProductMapper(
    IntegrationDbContext dbContext,
    IMinioFileStorage fileStorage,
    ILogger<TrendyolProductMapper> logger) : ITrendyolProductMapper
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<TrendyolCreateProductRequest>> MapProductAsync(Guid productId)
    {
        // Ürünü tüm ilişkileriyle çek
        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks).ThenInclude(s => s.BranchOffice)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.AttributeKeyValues)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Ürün bulunamadı.");

        if (product.ProductVariants.Count == 0)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Ürünün varyantı yok.");

        // Marketplace eşleştirmeleri — brand, category
        var brandMatch = product.BrandId.HasValue
            ? await dbContext.BrandMarketPlaceMatches.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ApplicationBrandId == product.BrandId && m.MarketPlaceId == TrendyolMarketPlaceId)
            : null;

        if (brandMatch is null)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Marka Trendyol eşleştirmesi bulunamadı.");

        var categoryMatch = await dbContext.CategoryMarketPlaceMatches.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ApplicationCategoryId == product.CategoryId && m.MarketPlaceId == TrendyolMarketPlaceId);

        if (categoryMatch is null)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Kategori Trendyol eşleştirmesi bulunamadı.");

        // Attribute eşleştirmelerini toplu çek
        var attributeIds = product.AttributeKeyValues.Select(a => a.CategoryAttributeId).Distinct().ToList();
        var attributeMatches = await dbContext.CategoryAttributeMarketPlaceMatches.AsNoTracking()
            .Where(m => attributeIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == TrendyolMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeId);

        var valueIds = product.AttributeKeyValues
            .Where(a => a.AttributeValueId.HasValue)
            .Select(a => a.AttributeValueId!.Value)
            .Distinct().ToList();
        var valueMatches = await dbContext.CategoryAttributeValueMarketPlaceMatches.AsNoTracking()
            .Where(m => valueIds.Contains(m.ApplicationCategoryAttributeValueId) && m.MarketPlaceId == TrendyolMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeValueId, m => m.MarketPlaceCategoryAttributeValueId);

        // Marketplace'e stok gönderecek depo ID'lerini belirle
        var warehouseIds = await dbContext.MarketPlaceWarehouses.AsNoTracking()
            .Where(w => w.MarketPlaceId == TrendyolMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        // Fallback: MarketPlaceWarehouse kaydı yoksa IsDefaultMarketPlaceStock olan depoları kullan
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
            // Quantity: seçili depoların stok toplamı
            var quantity = variant.BranchOfficeStocks
                .Where(s => warehouseIds.Contains(s.BranchOfficeId))
                .Sum(s => s.CurrentStock);

            // Görseller: MinIO StorageKey → public URL
            var images = variant.Images
                .Where(i => !string.IsNullOrEmpty(i.StorageKey))
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new TrendyolProductImage(fileStorage.GetPublicUrl(i.StorageKey!)))
                .ToList();

            if (images.Count == 0)
            {
                logger.LogWarning("Varyant {Barcode} görseli yok, atlanıyor", variant.Barcode);
                continue;
            }

            // SalePrice ≤ ListPrice kontrolü
            var salePrice = variant.SalePrice > variant.ListPrice ? variant.ListPrice : variant.SalePrice;

            // VatRate: 0, 1, 10, 20 olmalı
            var vatRate = (int)variant.VatRate;
            if (vatRate is not (0 or 1 or 10 or 20))
            {
                logger.LogWarning("Varyant {Barcode} geçersiz VatRate={VatRate}, 20 olarak ayarlandı", variant.Barcode, vatRate);
                vatRate = 20;
            }

            // Attributes: ürün seviyesi (AttributeKeyValues) + varyant seviyesi (ProductVariantAttributes)
            var attributes = new List<TrendyolProductAttribute>();

            // Ürün seviyesi özellikler
            foreach (var akv in product.AttributeKeyValues)
            {
                if (!attributeMatches.TryGetValue(akv.CategoryAttributeId, out var trendyolAttrId))
                    continue;

                int? trendyolValueId = null;
                string? customValue = null;

                if (akv.AttributeValueId.HasValue && valueMatches.TryGetValue(akv.AttributeValueId.Value, out var mappedValueId))
                {
                    trendyolValueId = mappedValueId;
                }
                else if (!string.IsNullOrEmpty(akv.CustomValue))
                {
                    customValue = akv.CustomValue;
                }
                else
                {
                    continue;
                }

                attributes.Add(new TrendyolProductAttribute(trendyolAttrId, trendyolValueId, customValue));
            }

            // Varyant seviyesi özellikler (renk, beden vb.)
            if (variant.ProductVariantAttributes is not null)
            {
                foreach (var pva in variant.ProductVariantAttributes)
                {
                    if (pva.CategoryAttributeValueId.HasValue
                        && valueMatches.TryGetValue(pva.CategoryAttributeValueId.Value, out var trendyolValueId))
                    {
                        // CategoryAttributeValueId üzerinden attribute match'i bul
                        // Value match'ten attribute'u resolve etmek için reverse lookup gerekiyor
                        // Ancak variant attribute'lar zaten match tablosundaki value'lar ile eşleşir
                        // Attribute ID'yi value üzerinden bulmak gerekiyor
                        var attrValue = await dbContext.CategoryAttributeValues.AsNoTracking()
                            .FirstOrDefaultAsync(v => v.Id == pva.CategoryAttributeValueId.Value);

                        if (attrValue is not null && attributeMatches.TryGetValue(attrValue.CategoryAttributeId, out var tAttrId))
                        {
                            attributes.Add(new TrendyolProductAttribute(tAttrId, trendyolValueId, null));
                        }
                    }
                    else if (!string.IsNullOrEmpty(pva.CustomValue) && pva.CategoryAttributeValueId.HasValue)
                    {
                        var attrValue = await dbContext.CategoryAttributeValues.AsNoTracking()
                            .FirstOrDefaultAsync(v => v.Id == pva.CategoryAttributeValueId.Value);

                        if (attrValue is not null && attributeMatches.TryGetValue(attrValue.CategoryAttributeId, out var tAttrId))
                        {
                            attributes.Add(new TrendyolProductAttribute(tAttrId, null, pva.CustomValue));
                        }
                    }
                }
            }

            var item = new TrendyolProductItem(
                Barcode: variant.Barcode,
                Title: product.Title.Length > 100 ? product.Title[..100] : product.Title,
                ProductMainId: productMainId,
                BrandId: brandMatch.MarketPlaceBrandId,
                CategoryId: categoryMatch.MarketPlaceCategoryId,
                ListPrice: variant.ListPrice,
                SalePrice: salePrice,
                VatRate: vatRate,
                StockCode: variant.Barcode,
                DimensionalWeight: variant.DimensionalWeight,
                Description: product.Description.Length > 30000 ? product.Description[..30000] : product.Description,
                Quantity: quantity,
                Images: images,
                Attributes: attributes);

            items.Add(item);
        }

        if (items.Count == 0)
            return new ErrorDataResult<TrendyolCreateProductRequest>(null!, "Hiçbir varyant Trendyol'a gönderilemedi (görsel eksik olabilir).");

        logger.LogInformation("Product {ProductId} mapped to {ItemCount} TrendyolProductItems", productId, items.Count);
        return new SuccessDataResult<TrendyolCreateProductRequest>(new TrendyolCreateProductRequest(items));
    }
}
