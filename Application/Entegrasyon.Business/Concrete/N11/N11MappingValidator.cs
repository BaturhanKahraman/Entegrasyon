using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.N11;

public class N11MappingValidator(IDbContextFactory<IntegrationDbContext> contextFactory)
{
    public virtual async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.BrandId, p.Title })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorResult("Urun bulunamadı.");

        var errors = new List<string>();

        // 1. Kategori eslestirmesi
        var categoryMapped = await dbContext.CategoryMarketplaces
            .AnyAsync(m => m.CategoryId == product.CategoryId && m.MarketPlaceId == N11MarketPlaceId && m.IsActive);
        if (!categoryMapped)
            errors.Add("Urunun kategorisi N11'e eslestirilmemis.");

        // 2. Marka eslestirmesi — N11'de marka opsiyoneldir; null ise hata verilmez
        if (product.BrandId is not null)
        {
            var brandMapped = await dbContext.BrandMarketPlaceMatches
                .AnyAsync(m => m.ApplicationBrandId == product.BrandId.Value && m.MarketPlaceId == N11MarketPlaceId);
            if (!brandMapped)
                errors.Add("Urunun markasi N11'e eslestirilmemis.");
        }

        // 3. Zorunlu ozellik eslestirmeleri
        var requiredAttrIds = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
            .Select(cac => cac.CategoryAttributeId)
            .ToListAsync();

        if (requiredAttrIds.Count > 0)
        {
            var mappedAttrIds = await dbContext.CategoryAttributeMarketPlaceMatches
                .Where(m => requiredAttrIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == N11MarketPlaceId)
                .Select(m => m.ApplicationCategoryAttributeId)
                .ToListAsync();

            var unmappedCount = requiredAttrIds.Count - mappedAttrIds.Count;
            if (unmappedCount > 0)
                errors.Add($"{unmappedCount} zorunlu ozellik N11'e eslestirilmemis.");
        }

        if (errors.Count > 0)
            return new ErrorResult($"'{product.Title}' N11'e gonderilemez: {string.Join(" ", errors)}");

        return new SuccessResult();
    }
}
