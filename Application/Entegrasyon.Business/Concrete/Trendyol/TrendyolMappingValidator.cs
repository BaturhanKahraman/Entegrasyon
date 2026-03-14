using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class TrendyolMappingValidator(IntegrationDbContext dbContext)
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.BrandId, p.Title })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var errors = new List<string>();

        // 1. Kategori eşleştirmesi
        var categoryMapped = await dbContext.CategoryMarketPlaceMatches
            .AnyAsync(m => m.ApplicationCategoryId == product.CategoryId && m.MarketPlaceId == TrendyolMarketPlaceId);
        if (!categoryMapped)
            errors.Add("Ürünün kategorisi Trendyol'a eşleştirilmemiş.");

        // 2. Marka eşleştirmesi
        if (product.BrandId is null)
        {
            errors.Add("Ürünün markası belirlenmemiş.");
        }
        else
        {
            var brandMapped = await dbContext.BrandMarketPlaceMatches
                .AnyAsync(m => m.ApplicationBrandId == product.BrandId.Value && m.MarketPlaceId == TrendyolMarketPlaceId);
            if (!brandMapped)
                errors.Add("Ürünün markası Trendyol'a eşleştirilmemiş.");
        }

        // 3. Zorunlu özellik eşleştirmeleri
        var requiredAttrIds = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
            .Select(cac => cac.CategoryAttributeId)
            .ToListAsync();

        if (requiredAttrIds.Count > 0)
        {
            var mappedAttrIds = await dbContext.CategoryAttributeMarketPlaceMatches
                .Where(m => requiredAttrIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == TrendyolMarketPlaceId)
                .Select(m => m.ApplicationCategoryAttributeId)
                .ToListAsync();

            var unmappedCount = requiredAttrIds.Count - mappedAttrIds.Count;
            if (unmappedCount > 0)
                errors.Add($"{unmappedCount} zorunlu özellik Trendyol'a eşleştirilmemiş.");
        }

        if (errors.Count > 0)
            return new ErrorResult($"'{product.Title}' gönderilemez: {string.Join(" ", errors)}");

        return new SuccessResult();
    }
}
