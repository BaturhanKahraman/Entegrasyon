using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed class PazaramaMappingValidator(IDbContextFactory<IntegrationDbContext> contextFactory)
{
    public async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.BrandId, p.Title })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var errors = new List<string>();

        // 1. Kategori eşleştirmesi — CategoryMarketplaces tablosu, ExternalCategoryId != null
        var categoryMapped = await dbContext.CategoryMarketplaces
            .AnyAsync(m => m.CategoryId == product.CategoryId
                        && m.MarketPlaceId == PazaramaMarketPlaceId
                        && m.ExternalCategoryId != null);
        if (!categoryMapped)
            errors.Add("Ürünün kategorisi Pazarama'ya eşleştirilmemiş.");

        // 2. Marka eşleştirmesi — Pazarama'da marka zorunludur
        if (product.BrandId is null)
        {
            errors.Add("Ürünün markası belirlenmemiş.");
        }
        else
        {
            var brandMapped = await dbContext.BrandMarketPlaceMatches
                .AnyAsync(m => m.ApplicationBrandId == product.BrandId.Value
                            && m.MarketPlaceId == PazaramaMarketPlaceId
                            && m.MarketPlaceBrandExternalId != null);
            if (!brandMapped)
                errors.Add("Ürünün markası Pazarama'ya eşleştirilmemiş.");
        }

        // 3. Zorunlu özellik eşleştirmeleri
        var requiredAttrIds = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
            .Select(cac => cac.CategoryAttributeId)
            .ToListAsync();

        if (requiredAttrIds.Count > 0)
        {
            var mappedAttrIds = await dbContext.CategoryAttributeMarketPlaceMatches
                .Where(m => requiredAttrIds.Contains(m.ApplicationCategoryAttributeId)
                         && m.MarketPlaceId == PazaramaMarketPlaceId
                         && m.MarketPlaceCategoryAttributeExternalId != null)
                .Select(m => m.ApplicationCategoryAttributeId)
                .ToListAsync();

            var unmappedCount = requiredAttrIds.Count - mappedAttrIds.Count;
            if (unmappedCount > 0)
                errors.Add($"{unmappedCount} zorunlu özellik Pazarama'ya eşleştirilmemiş.");
        }

        if (errors.Count > 0)
            return new ErrorResult($"'{product.Title}' Pazarama'ya gönderilemez: {string.Join(" ", errors)}");

        return new SuccessResult();
    }
}
