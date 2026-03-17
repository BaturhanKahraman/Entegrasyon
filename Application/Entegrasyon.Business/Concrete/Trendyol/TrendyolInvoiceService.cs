using System.Net.Http.Headers;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol fatura link/dosya gonderme servisi.
/// Fatura gonderilmeden kargo etiketi bastirilmaz.
/// </summary>
public sealed class TrendyolInvoiceService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolApiClient apiClient,
    ILogger<TrendyolInvoiceService> logger) : ITrendyolInvoiceService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IResult> SendInvoiceLinkAsync(long shipmentPackageId, string invoiceLink,
        long? invoiceDateTime = null, string? invoiceNumber = null)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamis.");

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
            return new ErrorResult($"Fatura gonderme hatasi: {response.StatusCode}");
        }

        logger.LogInformation("Invoice link sent for package {PackageId}", shipmentPackageId);
        return new SuccessResult("Fatura linki gonderildi.");
    }

    public async Task<IResult> DeleteInvoiceLinkAsync(long serviceSourceId, long customerId)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamis.");

        var url = $"integration/order/sellers/{sellerId}/seller-invoice-links/{serviceSourceId}/customers/{customerId}";
        var response = await apiClient.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
            return new ErrorResult($"Fatura silme hatasi: {response.StatusCode}");

        return new SuccessResult("Fatura linki silindi.");
    }

    public async Task<IResult> UploadInvoiceFileAsync(long shipmentPackageId, Stream file, string contentType)
    {
        var sellerId = await GetSellerIdAsync();
        if (sellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamis.");

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
            return new ErrorResult($"Fatura yukleme hatasi: {response.StatusCode}");
        }

        return new SuccessResult("Fatura dosyasi yuklendi.");
    }

    private async Task<string?> GetSellerIdAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);
        return marketplace?.SellerId;
    }
}
