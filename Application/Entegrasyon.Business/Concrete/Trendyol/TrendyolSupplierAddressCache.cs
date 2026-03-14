using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol satıcı adres bilgilerini cache'ler.
/// 1 req/hour rate limit — memory cache + DB fallback.
/// Ürün publish için ShipmentAddressId ve ReturningAddressId gerekli.
/// </summary>
public sealed class TrendyolSupplierAddressCache(
    IntegrationDbContext dbContext,
    ITrendyolApiClient apiClient,
    IMemoryCache memoryCache,
    ILogger<TrendyolSupplierAddressCache> logger)
{
    private const int TrendyolMarketPlaceId = 1;
    private const string CacheKey = "TrendyolSupplierAddresses";

    public async Task<TrendyolSupplierAddress?> GetDefaultAddressAsync()
    {
        if (memoryCache.TryGetValue<List<TrendyolSupplierAddress>>(CacheKey, out var cached) && cached?.Count > 0)
            return cached.FirstOrDefault(a => a.IsDefault) ?? cached.First();

        try
        {
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
