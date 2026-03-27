using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol satici adres bilgilerini cache'ler.
/// 1 req/hour rate limit -- memory cache + DB fallback.
/// Urun publish icin ShipmentAddressId ve ReturningAddressId gerekli.
/// </summary>
public sealed class TrendyolSupplierAddressCache(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolApiClient apiClient,
    TenantMemoryCache memoryCache,
    ILogger<TrendyolSupplierAddressCache> logger)
{
    private const string CacheKey = "TrendyolSupplierAddresses";

    public async Task<TrendyolSupplierAddress?> GetDefaultAddressAsync()
    {
        if (memoryCache.TryGetValue<List<TrendyolSupplierAddress>>(CacheKey, out var cached) && cached?.Count > 0)
            return cached.FirstOrDefault(a => a.IsDefault) ?? cached.First();

        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var marketplace = await dbContext.MarketPlaces.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

            if (marketplace?.SellerId is null) return null;

            var url = $"integration/sellers/{marketplace.SellerId}/addresses";
            var response = await apiClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var addressResponse = await response.Content.ReadFromJsonAsync<TrendyolAddressListResponse>();
            var addresses = addressResponse?.SupplierAddresses ?? [];

            memoryCache.Set(CacheKey, addresses, TimeSpan.FromHours(1));
            return addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Trendyol supplier addresses");
            return null;
        }
    }
}

public sealed record TrendyolAddressListResponse(List<TrendyolSupplierAddress>? SupplierAddresses);

public sealed record TrendyolSupplierAddress(
    int Id,
    string? FullAddress,
    string? City,
    string? District,
    string? PostalCode,
    bool IsDefault,
    bool IsShipmentAddress,
    bool IsInvoiceAddress,
    bool IsReturningAddress);
