using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol API üzerinden marketplace arama yapan servis.
/// Markalar için Trendyol API'ye istek atar; diğer marketplace'ler için DB fallback kullanır.
/// </summary>
public sealed class TrendyolMarketplaceSearchService(
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ITrendyolCategoryImportService categoryImportService,
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolMarketplaceSearchService> logger) : IMarketplaceSearchService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<List<MarketplaceCategorySearchResult>>> SearchCategoriesAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        try
        {
            var categoriesResult = await categoryImportService.GetTrendyolCategories();
            if (!categoriesResult.Success || categoriesResult.Data is null)
                return new ErrorDataResult<List<MarketplaceCategorySearchResult>>([], categoriesResult.Message!);

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

    public async Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        if (marketPlaceId == TrendyolMarketPlaceId)
            return await SearchBrandsTrendyolAsync(query, ct);

        return await SearchBrandsFromDbAsync(marketPlaceId, query, ct);
    }

    public async Task<IDataResult<List<MarketplaceAttributeSearchResult>>> SearchAttributesAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

            var attrs = await dbContext.CategoryAttributes
                .Where(a => string.IsNullOrWhiteSpace(query) ||
                            a.CategoryAttributeHumanized!.Contains(query) ||
                            a.CategoryAttributeKey!.Contains(query))
                .Take(20)
                .Select(a => new MarketplaceAttributeSearchResult(a.Id, a.CategoryAttributeHumanized!))
                .ToListAsync(ct);

            return new SuccessDataResult<List<MarketplaceAttributeSearchResult>>(attrs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol özellik arama hatası");
            return new ErrorDataResult<List<MarketplaceAttributeSearchResult>>([], "Özellik arama sırasında hata oluştu.");
        }
    }

    private async Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsTrendyolAsync(
        string query, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
            var url = $"product/brands/by-name?name={Uri.EscapeDataString(query)}&size=20";

            var response = await client.GetFromJsonAsync<TrendyolBrandsResponse>(url, ct);
            var brands = response?.Brands
                .Select(b => new MarketplaceBrandSearchResult(b.Id, b.Name))
                .ToList() ?? [];

            return new SuccessDataResult<List<MarketplaceBrandSearchResult>>(brands);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol marka arama hatasi");
            return new ErrorDataResult<List<MarketplaceBrandSearchResult>>([], "Marka arama sirasinda hata olustu.");
        }
    }

    private async Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsFromDbAsync(
        int marketPlaceId, string query, CancellationToken ct)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

            var q = dbContext.BrandMarketPlaceMatches
                .Where(m => m.MarketPlaceId == marketPlaceId);

            if (!string.IsNullOrWhiteSpace(query))
                q = q.Where(m => m.MarketPlaceBrandExternalId != null &&
                                  m.MarketPlaceBrandExternalId.Contains(query));

            var brands = await q
                .Select(m => new MarketplaceBrandSearchResult(
                    m.MarketPlaceBrandId,
                    m.MarketPlaceBrandExternalId ?? $"Brand #{m.MarketPlaceBrandId}"))
                .Distinct()
                .Take(20)
                .ToListAsync(ct);

            return new SuccessDataResult<List<MarketplaceBrandSearchResult>>(brands);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DB marka arama hatasi, marketPlaceId={MarketPlaceId}", marketPlaceId);
            return new ErrorDataResult<List<MarketplaceBrandSearchResult>>([], "Marka arama sirasinda hata olustu.");
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

    // ─── Private response models ───────────────────────────────────────────

    private record TrendyolBrandsResponse
    {
        [JsonPropertyName("brands")]
        public List<TrendyolBrandItem> Brands { get; init; } = [];
    }

    private record TrendyolBrandItem
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }
}
