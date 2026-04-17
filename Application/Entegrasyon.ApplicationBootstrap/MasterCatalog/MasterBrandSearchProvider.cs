using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.ApplicationBootstrap.MasterCatalog;

/// <summary>
/// AdminPanel master DB'sindeki MasterBrandMarketplaceMappings tablosundan marka arar.
/// </summary>
public sealed class MasterBrandSearchProvider(
    IDbContextFactory<AdminPanelDbContext> adminDbContextFactory,
    ILogger<MasterBrandSearchProvider> logger) : IMasterBrandSearchProvider
{
    public async Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        try
        {
            await using var adminDb = await adminDbContextFactory.CreateDbContextAsync(ct);

            var q = adminDb.MasterBrandMarketplaceMappings
                .Where(m => m.MarketplaceId == marketPlaceId);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLower();
                q = q.Where(m => (m.ExternalBrandName != null && m.ExternalBrandName.ToLower().Contains(term)) ||
                                  m.MasterBrand.Name.ToLower().Contains(term));
            }

            var brands = await q
                .Select(m => new MarketplaceBrandSearchResult(
                    m.ExternalBrandId,
                    m.ExternalBrandName ?? m.MasterBrand.Name))
                .Distinct()
                .Take(20)
                .ToListAsync(ct);

            return new SuccessDataResult<List<MarketplaceBrandSearchResult>>(brands);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Master DB marka arama hatası, marketPlaceId={MarketPlaceId}", marketPlaceId);
            return new SuccessDataResult<List<MarketplaceBrandSearchResult>>([]);
        }
    }
}
