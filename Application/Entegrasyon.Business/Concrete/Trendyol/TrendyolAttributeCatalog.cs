using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class TrendyolAttributeCatalog(
    ICategoryMatchService categoryMatchService,
    IEnumerable<IMarketplaceCategoryAttributeProvider> providers,
    TenantMemoryCache cache,
    ILogger<TrendyolAttributeCatalog> logger) : ITrendyolAttributeCatalog
{
    private const string CacheKey = "TrendyolAttributeCatalog";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<MarketplaceAttributeSearchResult>> SearchAttributesAsync(
        string query, CancellationToken ct = default)
    {
        var data = await GetCatalogAsync(ct);
        IEnumerable<MarketplaceAttributeSearchResult> attrs = data.Attributes;

        if (!string.IsNullOrWhiteSpace(query))
            attrs = attrs.Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        return attrs.Take(20).ToList();
    }

    public async Task<IReadOnlyList<MarketplaceOption>> SearchValuesAsync(
        int marketplaceAttributeId, string query, CancellationToken ct = default)
    {
        var data = await GetCatalogAsync(ct);
        if (!data.ValuesByAttributeId.TryGetValue(marketplaceAttributeId, out var values))
            return [];

        IEnumerable<MarketplaceOption> result = values;
        if (!string.IsNullOrWhiteSpace(query))
            result = result.Where(v => v.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        return result.Take(20).ToList();
    }

    private async Task<CatalogData> GetCatalogAsync(CancellationToken ct)
    {
        if (cache.TryGetValue<CatalogData>(CacheKey, out var cached) && cached is not null)
            return cached;

        var built = await BuildCatalogAsync(ct);
        cache.Set(CacheKey, built, Ttl);
        return built;
    }

    private async Task<CatalogData> BuildCatalogAsync(CancellationToken ct)
    {
        var provider = providers.FirstOrDefault(p => p.MarketPlaceId == TrendyolMarketPlaceId);
        if (provider is null)
        {
            logger.LogWarning("Trendyol attribute provider bulunamadı.");
            return CatalogData.Empty;
        }

        var mappings = await categoryMatchService.GetAllCategoryMappingsAsync(TrendyolMarketPlaceId);
        var categoryIds = mappings
            .Select(m => m.MarketPlaceCategoryId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0)
            return CatalogData.Empty;

        var attrById = new Dictionary<int, string>();
        var valuesByAttr = new Dictionary<int, Dictionary<int, string>>();

        // Sınırlı paralellik — stage API'yi boğmadan eşli kategorileri tara.
        using var gate = new SemaphoreSlim(4);
        var tasks = categoryIds.Select(async catId =>
        {
            await gate.WaitAsync(ct);
            try
            {
                return await provider.GetAttributesForCategoryAsync(catId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Trendyol kategori {CategoryId} attribute çekilemedi.", catId);
                return null;
            }
            finally { gate.Release(); }
        });

        var results = await Task.WhenAll(tasks);

        foreach (var res in results)
        {
            if (res is not { Success: true, Data: { } attrs }) continue;
            foreach (var a in attrs)
            {
                attrById[a.Id] = a.Name;
                if (!valuesByAttr.TryGetValue(a.Id, out var vmap))
                    valuesByAttr[a.Id] = vmap = new Dictionary<int, string>();
                foreach (var v in a.Values)
                    vmap[v.Id] = v.Name;
            }
        }

        var attributes = attrById
            .Select(kv => new MarketplaceAttributeSearchResult(kv.Key, kv.Value))
            .ToList();

        var values = valuesByAttr.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<MarketplaceOption>)kv.Value
                .Select(v => new MarketplaceOption(v.Key, v.Value))
                .ToList());

        return new CatalogData(attributes, values);
    }

    private sealed record CatalogData(
        IReadOnlyList<MarketplaceAttributeSearchResult> Attributes,
        IReadOnlyDictionary<int, IReadOnlyList<MarketplaceOption>> ValuesByAttributeId)
    {
        public static readonly CatalogData Empty = new([], new Dictionary<int, IReadOnlyList<MarketplaceOption>>());
    }
}
