using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Product + Variants -> List&lt;PazaramaProductItem&gt; donusumu.
/// Her varyant ayri bir PazaramaProductItem olarak olusturulur.
/// Tum ID'ler GUID formatinda ExternalId alanlarindan alinir.
/// </summary>
public sealed class PazaramaProductMapper(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<PazaramaProductMapper> logger) : IPazaramaProductMapper
{
    public async Task<IDataResult<PazaramaCreateProductRequest>> MapProductAsync(Guid productId)
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
            return new ErrorDataResult<PazaramaCreateProductRequest>(null!, "Urun bulunamadi.");

        if (product.ProductVariants.Count == 0)
            return new ErrorDataResult<PazaramaCreateProductRequest>(null!, "Urunun varyanti yok.");

        // Override'lari yukle (varsa)
        var marketplace = await dbContext.ProductMarketplaces
            .AsNoTracking()
            .Include(pm => pm.VariantOverrides)
            .FirstOrDefaultAsync(pm => pm.ProductId == productId
                && pm.MarketPlaceId == PazaramaMarketPlaceId && !pm.IsDeleted);

        var effectiveTitle = marketplace?.TitleOverride ?? product.Title;
        var effectiveDescription = marketplace?.DescriptionOverride ?? product.Description ?? string.Empty;

        // Marka eslestirmesi
        var brandMatch = product.BrandId.HasValue
            ? await dbContext.BrandMarketPlaceMatches.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ApplicationBrandId == product.BrandId && m.MarketPlaceId == PazaramaMarketPlaceId)
            : null;

        if (brandMatch is null || string.IsNullOrEmpty(brandMatch.MarketPlaceBrandExternalId))
            return new ErrorDataResult<PazaramaCreateProductRequest>(null!, "Marka Pazarama eslestirmesi bulunamadi.");

        // Kategori eslestirmesi -- CategoryMarketplaces (ExternalCategoryId GUID formatinda)
        var categoryMatch = await dbContext.CategoryMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.CategoryId == product.CategoryId && m.MarketPlaceId == PazaramaMarketPlaceId);

        if (categoryMatch is null || string.IsNullOrEmpty(categoryMatch.ExternalCategoryId))
            return new ErrorDataResult<PazaramaCreateProductRequest>(null!, "Kategori Pazarama eslestirmesi bulunamadi.");

        // Attribute eslestirmelerini toplu cek (GUID string olarak)
        var attributeIds = product.AttributeKeyValues.Select(a => a.CategoryAttributeId).Distinct().ToList();
        var attributeMatches = await dbContext.CategoryAttributeMarketPlaceMatches.AsNoTracking()
            .Where(m => attributeIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == PazaramaMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeExternalId ?? string.Empty);

        var valueIds = product.AttributeKeyValues
            .Where(a => a.AttributeValueId.HasValue)
            .Select(a => a.AttributeValueId!.Value)
            .Distinct().ToList();
        var valueMatches = await dbContext.CategoryAttributeValueMarketPlaceMatches.AsNoTracking()
            .Where(m => valueIds.Contains(m.ApplicationCategoryAttributeValueId) && m.MarketPlaceId == PazaramaMarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeValueId, m => m.MarketPlaceCategoryAttributeValueExternalId ?? string.Empty);

        // Marketplace'e stok gonderecek depo ID'lerini belirle
        var warehouseIds = await dbContext.MarketPlaceWarehouses.AsNoTracking()
            .Where(w => w.MarketPlaceId == PazaramaMarketPlaceId)
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

        // GroupCode: StockCode ilk 10 karakter, yoksa productId N format ilk 10 karakter
        var stockCode = product.StockCode;
        var groupCode = stockCode?.Length > 0
            ? stockCode[..Math.Min(10, stockCode.Length)]
            : productId.ToString("N")[..10];

        var hasMultipleVariants = product.ProductVariants.Count > 1;
        var items = new List<PazaramaProductItem>();
        var variantIndex = 0;

        foreach (var variant in product.ProductVariants)
        {
            variantIndex++;

            // Quantity: secili depolarin stok toplami
            var quantity = variant.BranchOfficeStocks
                .Where(s => warehouseIds.Contains(s.BranchOfficeId))
                .Sum(s => s.CurrentStock);

            // Gorseller: MinIO StorageKey -> public URL
            var images = variant.Images
                .Where(i => !string.IsNullOrEmpty(i.StorageKey))
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new PazaramaProductImage(fileStorage.GetPublicUrl(i.StorageKey!)))
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

            // Birden fazla varyant varsa isme varyant numarasi ekle
            var itemName = hasMultipleVariants
                ? $"{effectiveTitle} - Varyant {variantIndex}"
                : effectiveTitle;

            // Attributes: sadece onceden tanimli deger eslestirmesi olan ozellikler (Pazarama custom value desteklemiyor)
            var attributes = new List<PazaramaProductAttribute>();

            foreach (var akv in product.AttributeKeyValues)
            {
                if (!attributeMatches.TryGetValue(akv.CategoryAttributeId, out var pazaramaAttrId)
                    || string.IsNullOrEmpty(pazaramaAttrId))
                    continue;

                if (!akv.AttributeValueId.HasValue)
                    continue;

                if (!valueMatches.TryGetValue(akv.AttributeValueId.Value, out var pazaramaValueId)
                    || string.IsNullOrEmpty(pazaramaValueId))
                    continue;

                attributes.Add(new PazaramaProductAttribute(pazaramaAttrId, pazaramaValueId));
            }

            var item = new PazaramaProductItem(
                Name: itemName,
                DisplayName: itemName,
                Description: effectiveDescription,
                BrandId: brandMatch.MarketPlaceBrandExternalId!,
                Desi: 1,
                Code: variant.Barcode ?? variant.Id.ToString("N"),
                GroupCode: groupCode,
                StockCode: variant.Barcode ?? variant.Id.ToString("N"),
                StockCount: quantity,
                VatRate: (int)variant.VatRate,
                ListPrice: effectiveListPrice,
                SalePrice: salePrice,
                CategoryId: categoryMatch.ExternalCategoryId!,
                CurrencyType: "TRY",
                Images: images,
                Attributes: attributes);

            items.Add(item);
        }

        if (items.Count == 0)
            return new ErrorDataResult<PazaramaCreateProductRequest>(null!, "Hicbir varyant Pazarama'ya gonderilemedi (gorsel eksik olabilir).");

        logger.LogInformation("Product {ProductId} mapped to {ItemCount} PazaramaProductItems", productId, items.Count);
        return new SuccessDataResult<PazaramaCreateProductRequest>(new PazaramaCreateProductRequest(items));
    }
}
