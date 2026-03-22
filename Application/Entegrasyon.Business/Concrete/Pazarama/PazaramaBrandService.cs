using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama pazaryerinden marka listesini çeken ve yerel veritabanına aktaran servis.
/// GET /brand/getBrands?Page=1&amp;Size=100000 — GUID ID'li marka listesi döner.
/// </summary>
public sealed class PazaramaBrandService(
    IPazaramaApiClient apiClient,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<PazaramaBrandService> logger) : IPazaramaBrandService
{
    private const string BrandsEndpoint = "/brand/getBrands?Page=1&Size=100000";

    /// <summary>
    /// Pazarama API'sinden tüm markaları çeker. Opsiyonel isim filtresi uygulanabilir.
    /// </summary>
    public async Task<IDataResult<IEnumerable<PazaramaBrandDto>>> GetBrandsAsync(
        string? nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(nameFilter)
                ? BrandsEndpoint
                : $"{BrandsEndpoint}&name={nameFilter}";

            var response = await apiClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content
                .ReadFromJsonAsync<PazaramaResponse<List<PazaramaBrandDto>>>(
                    cancellationToken: cancellationToken);

            var brands = apiResponse?.Data ?? new List<PazaramaBrandDto>();

            logger.LogInformation("Pazarama'dan {Count} marka alındı", brands.Count);

            return new SuccessDataResult<IEnumerable<PazaramaBrandDto>>(brands);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama marka listesi çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<PazaramaBrandDto>>(
                Enumerable.Empty<PazaramaBrandDto>(),
                $"Hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Pazarama markalarını yerel veritabanına aktarır.
    /// Zaten eşleşmesi olan markalar atlanır.
    /// İsmi eşleşen marka varsa yalnızca match oluşturulur; yoksa hem Brand hem match eklenir.
    /// </summary>
    public async Task<IResult> ImportBrandsAsync(CancellationToken cancellationToken = default)
    {
        var getBrandsResult = await GetBrandsAsync(cancellationToken: cancellationToken);
        if (!getBrandsResult.Success)
            return new ErrorResult(getBrandsResult.Message ?? "Marka listesi alınamadı.");

        var brands = getBrandsResult.Data!.ToList();

        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var existingExternalIds = await dbContext.BrandMarketPlaceMatches
            .AsNoTracking()
            .Where(m => m.MarketPlaceId == PazaramaMarketPlaceId)
            .Select(m => m.MarketPlaceBrandExternalId)
            .ToListAsync(cancellationToken);

        var existingExternalIdSet = existingExternalIds.ToHashSet();

        int createdBrands = 0;
        int createdMatches = 0;
        int skipped = 0;

        foreach (var brandDto in brands)
        {
            var externalId = brandDto.Id.ToString();

            if (existingExternalIdSet.Contains(externalId))
            {
                skipped++;
                continue;
            }

            var existingBrand = await dbContext.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Name == brandDto.Name, cancellationToken);

            BrandMarketPlaceMatch match;

            if (existingBrand is not null)
            {
                // Brand already exists — only create the marketplace match
                match = new BrandMarketPlaceMatch
                {
                    ApplicationBrandId = existingBrand.Id,
                    MarketPlaceId = PazaramaMarketPlaceId,
                    MarketPlaceBrandId = 0,
                    MarketPlaceBrandExternalId = externalId
                };
            }
            else
            {
                // Brand doesn't exist — create brand + match together
                var newBrand = new Brand
                {
                    Name = brandDto.Name,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                match = new BrandMarketPlaceMatch
                {
                    ApplicationBrand = newBrand,
                    MarketPlaceId = PazaramaMarketPlaceId,
                    MarketPlaceBrandId = 0,
                    MarketPlaceBrandExternalId = externalId
                };

                createdBrands++;
            }

            dbContext.BrandMarketPlaceMatches.Add(match);
            createdMatches++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Pazarama marka import tamamlandı: {CreatedBrands} yeni marka, {CreatedMatches} yeni eşleşme, {Skipped} atlandı",
            createdBrands, createdMatches, skipped);

        return new SuccessResult(
            $"Import tamamlandı: {createdBrands} yeni marka, {createdMatches} eşleşme oluşturuldu, {skipped} atlandı.");
    }
}
