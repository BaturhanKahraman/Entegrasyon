using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// PttAVM urun mapping dogrulayicisi.
/// Urunun kategorisinin ve zorunlu ozelliklerinin PttAVM'ye eslestirildigini kontrol eder.
/// </summary>
public class PttavmMappingValidator(IDbContextFactory<IntegrationDbContext> contextFactory)
{
    public virtual async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.Title })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        // 1. Kategori eslestirmesi
        var categoryMapped = await dbContext.CategoryMarketplaces
            .AnyAsync(m => m.CategoryId == product.CategoryId && m.MarketPlaceId == PttavmMarketPlaceId);

        if (!categoryMapped)
            return new ErrorResult($"'{product.Title}' gönderilemez: Ürünün kategorisi PttAVM'ye eşleştirilmemiş.");

        // 2. Zorunlu ozellik eslestirmeleri
        var requiredAttrIds = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
            .Select(cac => cac.CategoryAttributeId)
            .ToListAsync();

        if (requiredAttrIds.Count > 0)
        {
            var mappedAttrIds = await dbContext.CategoryAttributeMarketPlaceMatches
                .Where(m => requiredAttrIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == PttavmMarketPlaceId)
                .Select(m => m.ApplicationCategoryAttributeId)
                .ToListAsync();

            var unmappedCount = requiredAttrIds.Count - mappedAttrIds.Count;
            if (unmappedCount > 0)
                return new ErrorResult($"'{product.Title}' gönderilemez: {unmappedCount} zorunlu özellik PttAVM'ye eşleştirilmemiş.");
        }

        return new SuccessResult();
    }
}
