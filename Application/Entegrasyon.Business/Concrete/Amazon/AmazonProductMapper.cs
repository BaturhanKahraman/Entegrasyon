using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

/// <summary>
/// İç ürün verisini Amazon Listings Items API formatına dönüştürür.
/// Product type'a uygun JSON Schema attribute mapping yapar.
/// </summary>
public sealed class AmazonProductMapper(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<AmazonProductMapper> logger) : IAmazonProductMapper
{
    private const int AmazonMpId = MarketPlaceConstants.AmazonMarketPlaceId;

    public async Task<IDataResult<AmazonListingItem>> MapProductAsync(
        Guid productId, string productType, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks)
            .Include(p => p.AttributeKeyValues)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
            return new ErrorDataResult<AmazonListingItem>(null!, "Ürün bulunamadı.");

        var variant = product.ProductVariants.FirstOrDefault();
        if (variant == null)
            return new ErrorDataResult<AmazonListingItem>(null!, "Ürünün varyantı yok.");

        var productMarketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == AmazonMpId, ct);

        // Attribute eşleşmeleri
        var attrMatches = await dbContext.CategoryAttributeMarketPlaceMatches
            .Where(m => m.MarketPlaceId == AmazonMpId && m.MarketPlaceCategoryAttributeExternalId != null)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeExternalId!, ct);

        var attributes = new Dictionary<string, object>();

        // Temel Amazon attribute'ları
        var title = productMarketplace?.TitleOverride ?? product.Title ?? "";
        var description = productMarketplace?.DescriptionOverride ?? product.Description ?? "";

        attributes["item_name"] = new[] { new { value = title.Length > 200 ? title[..200] : title, marketplace_id = "A33AVAJ2PDY3EV" } };
        attributes["brand"] = new[] { new { value = product.Brand?.Name ?? "" } };

        if (!string.IsNullOrWhiteSpace(description))
            attributes["product_description"] = new[] { new { value = description.Length > 2000 ? description[..2000] : description } };

        // EAN identifier
        if (!string.IsNullOrWhiteSpace(variant.Barcode))
            attributes["externally_assigned_product_identifier"] = new[] { new { type = "ean", value = variant.Barcode } };

        // Görseller
        var imageUrls = variant.Images.Take(9).Select(img => fileStorage.GetPublicUrl(img.StorageKey!)).ToList();
        if (imageUrls.Any())
            attributes["main_product_image_locator"] = new[] { new { media_location = imageUrls[0] } };
        for (var i = 1; i < imageUrls.Count; i++)
            attributes[$"other_product_image_locator_{i}"] = new[] { new { media_location = imageUrls[i] } };

        // Fiyat
        var salePrice = variant.SalePrice;
        attributes["purchasable_offer"] = new[] { new {
            currency = "TRY",
            our_price = new[] { new { schedule = new[] { new { value_with_tax = salePrice } } } },
            marketplace_id = "A33AVAJ2PDY3EV"
        }};

        // Stok
        var stock = variant.BranchOfficeStocks.Sum(s => s.CurrentStock);
        attributes["fulfillment_availability"] = new[] { new {
            fulfillment_channel_code = "DEFAULT",
            quantity = stock
        }};

        // Ürün attribute'ları (product-level)
        foreach (var akv in product.AttributeKeyValues)
        {
            if (attrMatches.TryGetValue(akv.CategoryAttributeId, out var amazonAttrId))
            {
                if (!string.IsNullOrWhiteSpace(akv.CustomValue))
                    attributes[amazonAttrId] = new[] { new { value = akv.CustomValue } };
            }
        }

        var listingItem = new AmazonListingItem(
            ProductType: productType,
            Requirements: "LISTING",
            Attributes: attributes);

        logger.LogInformation("Amazon product mapped: {ProductId}, productType={ProductType}", productId, productType);
        return new SuccessDataResult<AmazonListingItem>(listingItem);
    }
}
