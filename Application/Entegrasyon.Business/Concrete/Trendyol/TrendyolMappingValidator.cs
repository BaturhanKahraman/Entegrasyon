using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class TrendyolMappingValidator(IDbContextFactory<IntegrationDbContext> contextFactory)
{
    public async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.BrandId, p.Title })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorResult("Urun bulunamadi.");

        var errors = new List<string>();

        // 1. Kategori eslestirmesi
        var categoryMapped = await dbContext.CategoryMarketPlaceMatches
            .AnyAsync(m => m.ApplicationCategoryId == product.CategoryId && m.MarketPlaceId == TrendyolMarketPlaceId);
        if (!categoryMapped)
            errors.Add("Urunun kategorisi Trendyol'a eslestirilmemis.");

        // 2. Marka eslestirmesi
        if (product.BrandId is null)
        {
            errors.Add("Urunun markasi belirlenmemis.");
        }
        else
        {
            var brandMapped = await dbContext.BrandMarketPlaceMatches
                .AnyAsync(m => m.ApplicationBrandId == product.BrandId.Value && m.MarketPlaceId == TrendyolMarketPlaceId);
            if (!brandMapped)
                errors.Add("Urunun markasi Trendyol'a eslestirilmemis.");
        }

        // 3. Zorunlu ozellik eslestirmeleri
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
                errors.Add($"{unmappedCount} zorunlu ozellik Trendyol'a eslestirilmemis.");
        }

        if (errors.Count > 0)
            return new ErrorResult($"'{product.Title}' gonderilemez: {string.Join(" ", errors)}");

        return new SuccessResult();
    }
}
