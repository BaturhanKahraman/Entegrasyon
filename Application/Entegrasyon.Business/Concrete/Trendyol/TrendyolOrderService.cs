using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Gercek Trendyol Sipariş API cagrilari.
/// </summary>
public sealed class TrendyolOrderService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolApiClient apiClient,
    ILogger<TrendyolOrderService> logger) : ITrendyolOrderService
{
    public async Task<IDataResult<List<TrendyolShipmentPackage>>> FetchOrdersAsync(TrendyolOrderQueryParams query)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorDataResult<List<TrendyolShipmentPackage>>(null!, "Trendyol SellerId ayarlanmamis.");

        var url = $"integration/order/sellers/{sellerId}/orders?page={query.Page}&size={query.Size}";

        if (query.StartDate.HasValue)
            url += $"&startDate={query.StartDate.Value.ToUnixTimeMilliseconds()}";
        if (query.EndDate.HasValue)
            url += $"&endDate={query.EndDate.Value.ToUnixTimeMilliseconds()}";
        if (!string.IsNullOrEmpty(query.Status))
            url += $"&status={query.Status}";
        if (!string.IsNullOrEmpty(query.OrderByField))
            url += $"&orderByField={query.OrderByField}&orderByDirection={query.OrderByDirection ?? "DESC"}";

        var response = await apiClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol order fetch failed. Status={Status}, Body={Body}", response.StatusCode, errorBody);
            return new ErrorDataResult<List<TrendyolShipmentPackage>>(null!, $"Trendyol API hatasi: {response.StatusCode}");
        }

        var orderList = await response.Content.ReadFromJsonAsync<TrendyolOrderListResponse>();
        return new SuccessDataResult<List<TrendyolShipmentPackage>>(orderList?.Content ?? []);
    }

    public async Task<IResult> MarkUnsuppliedAsync(long shipmentPackageId, List<long> lineIds)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamis.");

        var url = $"integration/order/sellers/{sellerId}/shipment-packages/{shipmentPackageId}/unsupplied";
        var body = new { Lines = lineIds.Select(id => new { LineId = id, Quantity = 0 }).ToList() };
        var response = await apiClient.PutAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol unsupplied failed. PackageId={PackageId}, Body={Body}",
                shipmentPackageId, errorBody);
            return new ErrorResult($"Trendyol API hatasi: {response.StatusCode}");
        }

        return new SuccessResult("Sipariş tedarik edilemez olarak isaretlendi.");
    }

    public async Task<IResult> UpdateTrackingNumberAsync(long shipmentPackageId, string trackingNumber)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamis.");

        var url = $"integration/order/sellers/{sellerId}/shipment-packages/{shipmentPackageId}";
        var body = new { TrackingNumber = trackingNumber };
        var response = await apiClient.PutAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol tracking update failed. PackageId={PackageId}, Body={Body}",
                shipmentPackageId, errorBody);
            return new ErrorResult($"Trendyol API hatasi: {response.StatusCode}");
        }

        return new SuccessResult("Kargo takip numarasi guncellendi.");
    }

    public async Task<IDataResult<byte[]>> GetShippingLabelAsync(long shipmentPackageId)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorDataResult<byte[]>(null!, "Trendyol SellerId ayarlanmamis.");

        var url = $"integration/order/sellers/{sellerId}/shipment-packages/{shipmentPackageId}/shipping-label";
        var response = await apiClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new ErrorDataResult<byte[]>(null!, $"Kargo etiketi alinamadi: {response.StatusCode}");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return new SuccessDataResult<byte[]>(bytes);
    }

    private async Task<string?> GetSellerIdAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);
        return marketplace?.SellerId;
    }
}
