using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class MarketPlaceManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolApiClient apiClient,
    ILogger<MarketPlaceManager> logger) : IMarketPlaceManager
{
    public async Task<IDataResult<MarketPlace>> GetByIdAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var mp = await dbContext.MarketPlaces.FirstOrDefaultAsync(m => m.Id == id);
        if (mp is null)
            return new ErrorDataResult<MarketPlace>(null!, "Marketplace bulunamadı.");
        return new SuccessDataResult<MarketPlace>(mp);
    }

    public async Task<IDataResult<List<MarketPlace>>> GetAllAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var list = await dbContext.MarketPlaces.AsNoTracking().ToListAsync();
        return new SuccessDataResult<List<MarketPlace>>(list);
    }

    public async Task<IResult> UpdateCredentialsAsync(int id, string apiKey, string apiSecret, string sellerId, string? baseUrl)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var mp = await dbContext.MarketPlaces.FirstOrDefaultAsync(m => m.Id == id);
        if (mp is null)
            return new ErrorResult("Marketplace bulunamadı.");

        mp.ApiKey = apiKey;
        mp.ApiSecret = apiSecret;
        mp.SellerId = sellerId;
        mp.BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl.TrimEnd('/');
        mp.UserAgentPrefix = $"{sellerId} - SelfIntegration";

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Marketplace {Id} credentials updated. SellerId={SellerId}, BaseUrl={BaseUrl}",
            id, sellerId, mp.BaseUrl ?? "(default prod)");
        return new SuccessResult("Marketplace ayarları kaydedildi.");
    }

    public async Task<IDataResult<List<MarketPlaceWarehouse>>> GetWarehousesAsync(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var warehouses = await dbContext.MarketPlaceWarehouses
            .AsNoTracking()
            .Include(w => w.BranchOffice)
            .Where(w => w.MarketPlaceId == marketPlaceId)
            .ToListAsync();
        return new SuccessDataResult<List<MarketPlaceWarehouse>>(warehouses);
    }

    public async Task<IResult> SetWarehousesAsync(int marketPlaceId, List<int> branchOfficeIds)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // Mevcut kayıtları sil
        var existing = await dbContext.MarketPlaceWarehouses
            .Where(w => w.MarketPlaceId == marketPlaceId)
            .ToListAsync();
        dbContext.MarketPlaceWarehouses.RemoveRange(existing);

        // Yeni kayıtları ekle
        foreach (var branchId in branchOfficeIds)
        {
            dbContext.MarketPlaceWarehouses.Add(new MarketPlaceWarehouse
            {
                MarketPlaceId = marketPlaceId,
                BranchOfficeId = branchId
            });
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Marketplace {Id} warehouses updated: [{Warehouses}]",
            marketPlaceId, string.Join(", ", branchOfficeIds));
        return new SuccessResult("Depo ayarları kaydedildi.");
    }

    public async Task<IDataResult<bool>> TestConnectionAsync(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        try
        {
            var mp = await dbContext.MarketPlaces.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == marketPlaceId);

            if (mp?.SellerId is null)
                return new ErrorDataResult<bool>(false, "SellerId ayarlanmamış.");

            // Basit bir GET isteği ile bağlantıyı test et (satıcı adreslerini çek)
            var response = await apiClient.GetAsync($"integration/sellers/{mp.SellerId}/addresses");

            if (response.IsSuccessStatusCode)
                return new SuccessDataResult<bool>(true, "Bağlantı başarılı!");

            var body = await response.Content.ReadAsStringAsync();
            return new ErrorDataResult<bool>(false, $"API yanıtı: {response.StatusCode} — {body}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Trendyol connection test failed for marketplace {Id}", marketPlaceId);
            return new ErrorDataResult<bool>(false, $"Bağlantı hatası: {ex.Message}");
        }
    }
}
