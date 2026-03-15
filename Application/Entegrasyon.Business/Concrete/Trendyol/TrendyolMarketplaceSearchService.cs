using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol API üzerinden marketplace arama yapan servis.
/// Şu an skeleton — asıl API entegrasyonu sonra yapılacak.
/// </summary>
public sealed class TrendyolMarketplaceSearchService(
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ITrendyolCategoryImportService categoryImportService,
    ILogger<TrendyolMarketplaceSearchService> logger) : IMarketplaceSearchService
{
    public async Task<IDataResult<List<MarketplaceCategorySearchResult>>> SearchCategoriesAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        try
        {
            var categoriesResult = await categoryImportService.GetTrendyolCategories();
            if (!categoriesResult.Success || categoriesResult.Data is null)
                return new ErrorDataResult<List<MarketplaceCategorySearchResult>>([], categoriesResult.Message);

            var flat = new List<MarketplaceCategorySearchResult>();
            FlattenCategories(categoriesResult.Data, null, flat);

            if (!string.IsNullOrWhiteSpace(query))
            {
                flat = flat
                    .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                (c.FullPath?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();
            }

            return new SuccessDataResult<List<MarketplaceCategorySearchResult>>(flat.Take(20).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol kategori arama hatası");
            return new ErrorDataResult<List<MarketplaceCategorySearchResult>>([], "Kategori arama sırasında hata oluştu.");
        }
    }

    public Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        // TODO: Trendyol brands API entegrasyonu — GET brands/by-name?name={query}
        logger.LogWarning("TrendyolMarketplaceSearchService.SearchBrandsAsync henüz implemente edilmedi, boş liste dönüyor");
        return Task.FromResult<IDataResult<List<MarketplaceBrandSearchResult>>>(
            new SuccessDataResult<List<MarketplaceBrandSearchResult>>([]));
    }

    public async Task<IDataResult<List<MarketplaceAttributeSearchResult>>> SearchAttributesAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

            var attrs = await dbContext.CategoryAttributes
                .Where(a => string.IsNullOrWhiteSpace(query) ||
                            a.CategoryAttributeHumanized.Contains(query) ||
                            a.CategoryAttributeKey.Contains(query))
                .Take(20)
                .Select(a => new MarketplaceAttributeSearchResult(a.Id, a.CategoryAttributeHumanized))
                .ToListAsync(ct);

            return new SuccessDataResult<List<MarketplaceAttributeSearchResult>>(attrs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol özellik arama hatası");
            return new ErrorDataResult<List<MarketplaceAttributeSearchResult>>([], "Özellik arama sırasında hata oluştu.");
        }
    }

    private static void FlattenCategories(
        IEnumerable<ImportedTrendyolCategory> categories, string? parentPath,
        List<MarketplaceCategorySearchResult> results)
    {
        foreach (var cat in categories)
        {
            var fullPath = string.IsNullOrEmpty(parentPath) ? cat.Name : $"{parentPath} > {cat.Name}";
            results.Add(new MarketplaceCategorySearchResult(cat.Id, cat.Name, fullPath));

            if (cat.SubCategories is { Count: > 0 })
                FlattenCategories(cat.SubCategories, fullPath, results);
        }
    }
}
