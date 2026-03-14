using System.Net.Http.Headers;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol fatura link/dosya gönderme servisi.
/// Fatura gönderilmeden kargo etiketi bastırılamaz.
/// </summary>
public sealed class TrendyolInvoiceService(
    IntegrationDbContext dbContext,
    ITrendyolApiClient apiClient,
    ILogger<TrendyolInvoiceService> logger) : ITrendyolInvoiceService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IResult> SendInvoiceLinkAsync(long shipmentPackageId, string invoiceLink,
        long? invoiceDateTime = null, string? invoiceNumber = null)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamış.");

        var url = $"integration/order/sellers/{sellerId}/seller-invoice-links";
        var body = new
        {
            ShipmentPackageId = shipmentPackageId,
            InvoiceLink = invoiceLink,
            InvoiceDateTime = invoiceDateTime ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            InvoiceNumber = invoiceNumber
        };

        var response = await apiClient.PostAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol invoice link failed. PackageId={PackageId}, Body={Body}",
                shipmentPackageId, errorBody);
            return new ErrorResult($"Fatura gönderme hatası: {response.StatusCode}");
        }

        logger.LogInformation("Invoice link sent for package {PackageId}", shipmentPackageId);
        return new SuccessResult("Fatura linki gönderildi.");
    }

    public async Task<IResult> DeleteInvoiceLinkAsync(long serviceSourceId, long customerId)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamış.");

        var url = $"integration/order/sellers/{sellerId}/seller-invoice-links/{serviceSourceId}/customers/{customerId}";
        var response = await apiClient.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
            return new ErrorResult($"Fatura silme hatası: {response.StatusCode}");

        return new SuccessResult("Fatura linki silindi.");
    }

    public async Task<IResult> UploadInvoiceFileAsync(long shipmentPackageId, Stream file, string contentType)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamış.");

        // Bu endpoint multipart/form-data gerektirir — doğrudan HttpClient kullanır
        var url = $"integration/order/sellers/{sellerId}/shipment-packages/{shipmentPackageId}/invoice";

        using var formContent = new MultipartFormDataContent();
        var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        formContent.Add(streamContent, "invoice", "invoice.pdf");

        var response = await apiClient.PostAsync(url, formContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol invoice upload failed. PackageId={PackageId}, Body={Body}",
                shipmentPackageId, errorBody);
            return new ErrorResult($"Fatura yükleme hatası: {response.StatusCode}");
        }

        return new SuccessResult("Fatura dosyası yüklendi.");
    }

    private async Task<string?> GetSellerIdAsync()
    {
        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);
        return marketplace?.SellerId;
    }
}
