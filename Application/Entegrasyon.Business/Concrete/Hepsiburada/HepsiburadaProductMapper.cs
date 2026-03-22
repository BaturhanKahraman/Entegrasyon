using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// İç ürün verisini Hepsiburada JSON formatına dönüştürür.
/// HB-spesifik: merchantSku UPPER, fiyat virgüllü, attribute string ID.
/// </summary>
public sealed class HepsiburadaProductMapper(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<HepsiburadaProductMapper> logger) : IHepsiburadaProductMapper
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    public async Task<IDataResult<List<HepsiburadaProductItem>>> MapProductAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks).ThenInclude(s => s.BranchOffice)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.AttributeKeyValues)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return new ErrorDataResult<List<HepsiburadaProductItem>>(null, "Ürün bulunamadı.");

        if (!product.ProductVariants.Any())
            return new ErrorDataResult<List<HepsiburadaProductItem>>(null, "Ürünün varyantı yok.");

        // Marketplace bilgileri
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == HbMarketPlaceId);

        if (marketplace == null)
            return new ErrorDataResult<List<HepsiburadaProductItem>>(null, "Hepsiburada marketplace kaydı bulunamadı.");

        var merchantId = marketplace.SellerId ?? "";

        // Kategori marketplace eşleşmesi
        var categoryMatch = await dbContext.CategoryMarketplaces
            .FirstOrDefaultAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == HbMarketPlaceId);

        if (categoryMatch == null)
            return new ErrorDataResult<List<HepsiburadaProductItem>>(null, "Kategori Hepsiburada'ya eşleştirilmemiş.");

        // Marketplace override'ları
        var productMarketplace = await dbContext.ProductMarketplaces
            .Include(pm => pm.VariantOverrides)
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == HbMarketPlaceId);

        // Attribute eşleşmeleri
        var attrMatches = await dbContext.CategoryAttributeMarketPlaceMatches
            .Where(m => m.MarketPlaceId == HbMarketPlaceId && m.MarketPlaceCategoryAttributeExternalId != null)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeExternalId!);

        var valueMatches = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .Where(m => m.MarketPlaceId == HbMarketPlaceId && m.MarketPlaceCategoryAttributeValueExternalId != null)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeValueId, m => m.MarketPlaceCategoryAttributeValueExternalId!);

        // Warehouse config
        var warehouseBranchIds = await dbContext.MarketPlaceWarehouses
            .Where(w => w.MarketPlaceId == HbMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        if (!warehouseBranchIds.Any())
        {
            var defaultBranch = await dbContext.BranchOffices
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();
            if (defaultBranch > 0) warehouseBranchIds.Add(defaultBranch);
        }

        var items = new List<HepsiburadaProductItem>();

        foreach (var variant in product.ProductVariants)
        {
            var attributes = new Dictionary<string, object>();

            // Temel alanlar
            var sku = (variant.Barcode ?? $"{product.Id}-{variant.Id}").ToUpperInvariant().Replace(" ", "");
            attributes["merchantSku"] = sku;
            attributes["VaryantGroupID"] = product.Id.ToString("N")[..16];
            attributes["Barcode"] = variant.Barcode ?? "";

            // Title & Description (override desteği)
            var title = productMarketplace?.TitleOverride ?? product.Title ?? "";
            var description = productMarketplace?.DescriptionOverride ?? product.Description ?? "";
            attributes["UrunAdi"] = title.Length > 500 ? title[..500] : title;
            attributes["UrunAciklamasi"] = description.Length > 30000 ? description[..30000] : description;

            // Marka (HB'de attribute olarak gidiyor)
            attributes["Marka"] = product.Brand?.Name ?? "";

            // Fiyat (virgüllü format)
            var variantOverride = productMarketplace?.VariantOverrides
                ?.FirstOrDefault(vo => vo.ProductVariantId == variant.Id);
            var listPrice = variantOverride?.ListPriceOverride ?? variant.ListPrice;
            var salePrice = variantOverride?.SalePriceOverride ?? variant.SalePrice;
            attributes["price"] = FormatPrice(salePrice);

            // Stok
            var stock = variant.BranchOfficeStocks
                .Where(s => warehouseBranchIds.Contains(s.BranchOfficeId))
                .Sum(s => s.CurrentStock);
            attributes["stock"] = stock.ToString();

            // KDV oranı (variant'tan)
            attributes["tax_vat_rate"] = variant.VatRate > 0 ? variant.VatRate.ToString("F0") : "20";

            // Desi
            attributes["kg"] = (variant.DimensionalWeight > 0 ? variant.DimensionalWeight : 1m).ToString("F1");

            // Garanti süresi (ay, default 24)
            attributes["GarantiSuresi"] = "24";

            // Görseller (max 5)
            var imageIndex = 1;
            foreach (var image in variant.Images.Take(5))
            {
                var publicUrl = fileStorage.GetPublicUrl(image.StorageKey);
                attributes[$"Image{imageIndex}"] = publicUrl;
                imageIndex++;
            }

            // Ürün attribute'ları (product-level)
            foreach (var akv in product.AttributeKeyValues)
            {
                if (attrMatches.TryGetValue(akv.CategoryAttributeId, out var hbAttrId))
                {
                    if (akv.AttributeValueId.HasValue && akv.AttributeValueId.Value > 0 &&
                        valueMatches.TryGetValue(akv.AttributeValueId.Value, out var hbValueId))
                    {
                        attributes[hbAttrId] = hbValueId;
                    }
                    else if (!string.IsNullOrWhiteSpace(akv.CustomValue))
                    {
                        attributes[hbAttrId] = akv.CustomValue;
                    }
                }
            }

            items.Add(new HepsiburadaProductItem(
                CategoryId: categoryMatch.MarketPlaceCategoryId,
                Merchant: merchantId,
                Attributes: attributes));
        }

        logger.LogInformation("HB product mapped: {ProductId}, {ItemCount} items", productId, items.Count);
        return new SuccessDataResult<List<HepsiburadaProductItem>>(items);
    }

    /// <summary>
    /// Decimal → virgüllü string formatı (HB gereksinimi: "130,50")
    /// </summary>
    private static string FormatPrice(decimal price) =>
        price.ToString("F2").Replace('.', ',');
}
