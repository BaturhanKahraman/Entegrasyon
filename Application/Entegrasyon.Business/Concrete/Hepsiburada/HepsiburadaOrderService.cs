using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Hepsiburada sipariş yönetimi servisi.
/// Order API ayrı base URL: oms-external.hepsiburada.com
/// </summary>
public sealed class HepsiburadaOrderService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHepsiburadaApiClient apiClient,
    ILogger<HepsiburadaOrderService> logger) : IHepsiburadaOrderService
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    private async Task<string> GetMerchantIdAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == HbMarketPlaceId)
            ?? throw new InvalidOperationException("Hepsiburada marketplace kaydı bulunamadı.");
        return marketplace.SellerId ?? throw new InvalidOperationException("Hepsiburada merchantId tanımlı değil.");
    }

    public async Task<IDataResult<List<HepsiburadaOrderDto>>> GetOrdersAsync(
        DateTimeOffset? beginDate = null, DateTimeOffset? endDate = null,
        int offset = 0, int limit = 50)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var url = $"/orders/merchantid/{merchantId}?limit={limit}&offset={offset}";

            if (beginDate.HasValue)
                url += $"&begindate={beginDate.Value:yyyy-MM-dd}";
            if (endDate.HasValue)
                url += $"&enddate={endDate.Value:yyyy-MM-dd}";

            var response = await apiClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<List<HepsiburadaOrderDto>>(null, $"Sipariş API hatası: {response.StatusCode}");

            var data = await response.Content.ReadFromJsonAsync<HepsiburadaOrderListResponse>();
            return new SuccessDataResult<List<HepsiburadaOrderDto>>(data?.Content ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB order list failed");
            return new ErrorDataResult<List<HepsiburadaOrderDto>>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<HepsiburadaOrderDto>> GetOrderAsync(string orderNumber)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.GetAsync($"/orders/merchantid/{merchantId}/ordernumber/{orderNumber}");

            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<HepsiburadaOrderDto>(null, $"Sipariş bulunamadı: {response.StatusCode}");

            var order = await response.Content.ReadFromJsonAsync<HepsiburadaOrderDto>();
            return new SuccessDataResult<HepsiburadaOrderDto>(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB get order failed: {OrderNumber}", orderNumber);
            return new ErrorDataResult<HepsiburadaOrderDto>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<HepsiburadaPackageResponse>> CreatePackageAsync(HepsiburadaPackageRequest request)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.PostAsync($"/packages/merchantid/{merchantId}", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("HB package creation failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorDataResult<HepsiburadaPackageResponse>(null, $"Paketleme hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<HepsiburadaPackageResponse>();
            logger.LogInformation("HB package created: {PackageNumber}", result?.PackageNumber);
            return new SuccessDataResult<HepsiburadaPackageResponse>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB package creation exception");
            return new ErrorDataResult<HepsiburadaPackageResponse>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> CancelLineItemAsync(string lineItemId, int reasonId)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var request = new HepsiburadaCancelRequest(reasonId);
            var response = await apiClient.PostAsync(
                $"/lineitems/merchantid/{merchantId}/id/{lineItemId}/cancelbymerchant", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("HB cancel failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorResult($"İptal hatası: {response.StatusCode}");
            }

            logger.LogInformation("HB line item cancelled: {LineItemId}", lineItemId);
            return new SuccessResult("Sipariş kalemi iptal edildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB cancel exception: {LineItemId}", lineItemId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> AddInvoiceAsync(string lineItemId, HepsiburadaInvoiceRequest invoice)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.PostAsync(
                $"/lineitems/merchantid/{merchantId}/id/{lineItemId}/invoice", invoice);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("HB invoice add failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorResult($"Fatura ekleme hatası: {response.StatusCode}");
            }

            logger.LogInformation("HB invoice added: {LineItemId}", lineItemId);
            return new SuccessResult("Fatura eklendi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB invoice exception: {LineItemId}", lineItemId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
