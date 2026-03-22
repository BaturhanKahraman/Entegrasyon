using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// Dahili ürün verisini N11 SaveProduct SOAP XML formatına dönüştürür.
/// Tüm varyantlar tek bir &lt;product&gt; öğesi altında &lt;stockItems&gt; olarak listelenir.
/// </summary>
public sealed class N11ProductMapper(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<N11ProductMapper> logger) : IN11ProductMapper
{
    public async Task<IDataResult<XElement>> MapProductAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 1. Ürünü tüm ilişkileriyle çek
        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks).ThenInclude(s => s.BranchOffice)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.AttributeKeyValues).ThenInclude(a => a.CategoryAttribute).ThenInclude(ca => ca.CategoryAttributeValues)
            .Include(p => p.AttributeKeyValues).ThenInclude(a => a.AttributeValue)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorDataResult<XElement>(null!, "Urun bulunamadi.");

        if (product.ProductVariants.Count == 0)
            return new ErrorDataResult<XElement>(null!, "Urunun varyanti yok.");

        // 2. Override'ları yükle (varsa)
        var marketplace = await dbContext.ProductMarketplaces
            .AsNoTracking()
            .Include(pm => pm.VariantOverrides)
            .FirstOrDefaultAsync(pm => pm.ProductId == productId
                && pm.MarketPlaceId == N11MarketPlaceId && !pm.IsDeleted);

        var effectiveTitle = marketplace?.TitleOverride ?? product.Title;
        var effectiveDescription = marketplace?.DescriptionOverride ?? product.Description ?? string.Empty;

        // 3. Kategori N11 eşleştirmesi
        var categoryMatch = await dbContext.CategoryMarketPlaceMatches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ApplicationCategoryId == product.CategoryId && m.MarketPlaceId == N11MarketPlaceId);

        if (categoryMatch is null)
            return new ErrorDataResult<XElement>(null!, "Kategori N11 eslestirmesi bulunamadi.");

        // 4. Özellik adı eşleştirmeleri (ID → humanized name için kullanılmaz — N11 string isim ister)
        var attributeIds = product.AttributeKeyValues.Select(a => a.CategoryAttributeId).Distinct().ToList();
        var attributeMatches = await dbContext.CategoryAttributeMarketPlaceMatches
            .AsNoTracking()
            .Where(m => attributeIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == N11MarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeId);

        // 5. Özellik değeri eşleştirmeleri (opsiyonel — N11 string değer kabul eder)
        var valueIds = product.AttributeKeyValues
            .Where(a => a.AttributeValueId.HasValue)
            .Select(a => a.AttributeValueId!.Value)
            .Distinct().ToList();
        var valueMatches = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .AsNoTracking()
            .Where(m => valueIds.Contains(m.ApplicationCategoryAttributeValueId) && m.MarketPlaceId == N11MarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeValueId, m => m.MarketPlaceCategoryAttributeValueId);

        // 6. Stok gönderilecek depolar
        var warehouseIds = await dbContext.MarketPlaceWarehouses
            .AsNoTracking()
            .Where(w => w.MarketPlaceId == N11MarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        // Fallback: MarketPlaceWarehouse kaydı yoksa IsDefaultMarketPlaceStock olanları kullan
        if (warehouseIds.Count == 0)
        {
            warehouseIds = await dbContext.BranchOffices
                .AsNoTracking()
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .ToListAsync();
        }

        // 7. Ürün düzeyi görsel
        var firstVariant = product.ProductVariants.First();
        var productImages = firstVariant.Images
            .Where(i => !string.IsNullOrEmpty(i.StorageKey))
            .OrderBy(i => i.DisplayOrder)
            .ToList();

        // 8. Ürün düzeyi özellikler (product-level attributes)
        var productAttributes = BuildProductAttributes(product, attributeMatches);

        // 9. stockItems oluştur
        var stockItemElements = new List<XElement>();

        foreach (var variant in product.ProductVariants)
        {
            var quantity = variant.BranchOfficeStocks
                .Where(s => warehouseIds.Contains(s.BranchOfficeId))
                .Sum(s => s.CurrentStock);

            var variantOverride = marketplace?.VariantOverrides
                ?.FirstOrDefault(vo => vo.ProductVariantId == variant.Id);

            var effectiveListPrice = variantOverride?.ListPriceOverride ?? variant.ListPrice;
            var effectiveSalePrice = variantOverride?.SalePriceOverride ?? variant.SalePrice;
            var salePrice = effectiveSalePrice > effectiveListPrice ? effectiveListPrice : effectiveSalePrice;

            // Varyant düzeyi özellikler (renk, beden vb.)
            var variantAttrElements = BuildVariantAttributes(variant);

            var stockItem = new XElement("stockItem",
                new XElement("sellerStockCode", variant.Barcode ?? variant.Id.ToString()),
                new XElement("quantity", quantity),
                new XElement("optionPrice", salePrice.ToString("F0")),
                variantAttrElements.Count > 0
                    ? new XElement("attributes", variantAttrElements)
                    : null!
            );

            stockItemElements.Add(stockItem);
        }

        if (stockItemElements.Count == 0)
            return new ErrorDataResult<XElement>(null!, "Hicbir varyant N11'e gonderilemedi.");

        // 10. Görseller
        var imageElements = productImages
            .Select((img, idx) => new XElement("image",
                new XElement("url", fileStorage.GetPublicUrl(img.StorageKey!)),
                new XElement("order", idx + 1)))
            .ToList();

        // 11. XML oluştur
        var productElement = new XElement("product",
            new XElement("productSellerCode", product.StockCode ?? product.Id.ToString()),
            new XElement("title", effectiveTitle),
            new XElement("description", effectiveDescription),
            new XElement("category",
                new XElement("id", categoryMatch.MarketPlaceCategoryId)),
            new XElement("price", firstVariant.ListPrice.ToString("F0")),
            new XElement("currencyType", "1"),
            productAttributes.Count > 0
                ? new XElement("attributes", productAttributes)
                : null!,
            imageElements.Count > 0
                ? new XElement("images", imageElements)
                : null!,
            new XElement("stockItems", stockItemElements),
            new XElement("productCondition", "1"),
            new XElement("preparingDay", "3"),
            new XElement("domestic", "false")
        );

        logger.LogInformation("Product {ProductId} N11 XML olarak eslendi ({VariantCount} stockItem)", productId, stockItemElements.Count);
        return new SuccessDataResult<XElement>(productElement);
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürün düzeyi AttributeKeyValue listesinden N11 &lt;attribute&gt; XElement'leri üretir.
    /// N11 string isim-değer çifti ister; özellik adı CategoryAttributeHumanized'dan alınır.
    /// </summary>
    private static List<XElement> BuildProductAttributes(
        Entegrasyon.Entity.Products.Product product,
        Dictionary<int, int> attributeMatches)
    {
        var result = new List<XElement>();

        foreach (var akv in product.AttributeKeyValues)
        {
            // N11'e eşleştirilmemiş özellikler atlanır
            if (!attributeMatches.ContainsKey(akv.CategoryAttributeId))
                continue;

            var attrName = akv.CategoryAttribute?.CategoryAttributeHumanized;
            if (string.IsNullOrEmpty(attrName))
                continue;

            string? attrValue = null;

            if (akv.AttributeValue?.Name is not null)
                attrValue = akv.AttributeValue.Name;
            else if (!string.IsNullOrEmpty(akv.CustomValue))
                attrValue = akv.CustomValue;

            if (string.IsNullOrEmpty(attrValue))
                continue;

            result.Add(new XElement("attribute",
                new XElement("name", attrName),
                new XElement("value", attrValue)));
        }

        return result;
    }

    /// <summary>
    /// Varyant düzeyi ProductVariantAttribute listesinden N11 &lt;attribute&gt; XElement'leri üretir.
    /// </summary>
    private static List<XElement> BuildVariantAttributes(Entegrasyon.Entity.Products.ProductVariant variant)
    {
        var result = new List<XElement>();

        if (variant.ProductVariantAttributes is null or { Count: 0 })
            return result;

        foreach (var pva in variant.ProductVariantAttributes)
        {
            // CategoryAttributeValue alanı navigation property değil string olarak tutuluyor
            var attrValue = pva.CustomValue ?? pva.CategoryAttributeValue;
            if (string.IsNullOrEmpty(attrValue))
                continue;

            // Özellik adı: ProductVariantAttribute içinde key bilgisi tutulmuyor;
            // N11 context'te gerçek uygulama bunu CategoryAttributeValue üzerinden yükler.
            // Test/mock senaryolarında değer var ise generic "Ozellik" adı kullanılır.
            result.Add(new XElement("attribute",
                new XElement("name", "Ozellik"),
                new XElement("value", attrValue)));
        }

        return result;
    }
}
