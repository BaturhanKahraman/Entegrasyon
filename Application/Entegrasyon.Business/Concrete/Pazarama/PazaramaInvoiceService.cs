using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama fatura servisi (gerçek API çağrıları).
/// InvoiceLink: POST /order/invoice-link
/// MultipleInvoiceLink: POST /order/multiple-invoice-link
/// </summary>
public sealed class PazaramaInvoiceService(
    IPazaramaApiClient apiClient,
    ILogger<PazaramaInvoiceService> logger) : IPazaramaInvoiceService
{
    public async Task<IResult> UploadInvoiceLinkAsync(PazaramaInvoiceLinkRequest request)
    {
        try
        {
            logger.LogInformation("Pazarama UploadInvoiceLink başlatıldı. OrderId: {OrderId}", request.OrderId);

            var response = await apiClient.PostAsync("order/invoice-link", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama UploadInvoiceLink başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<object>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Fatura yüklenemedi";
                logger.LogWarning("Pazarama UploadInvoiceLink yanıt başarısız: {Message}", msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("Pazarama UploadInvoiceLink başarılı. OrderId: {OrderId}", request.OrderId);
            return new SuccessResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama UploadInvoiceLink exception. OrderId: {OrderId}", request.OrderId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> UploadMultipleInvoiceLinkAsync(PazaramaMultipleInvoiceLinkRequest request)
    {
        try
        {
            logger.LogInformation("Pazarama UploadMultipleInvoiceLink başlatıldı. OrderId: {OrderId}, ItemCount: {Count}",
                request.OrderId, request.OrderItemIds?.Count ?? 0);

            var response = await apiClient.PostAsync("order/multiple-invoice-link", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama UploadMultipleInvoiceLink başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<object>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Çoklu fatura yüklenemedi";
                logger.LogWarning("Pazarama UploadMultipleInvoiceLink yanıt başarısız: {Message}", msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("Pazarama UploadMultipleInvoiceLink başarılı. OrderId: {OrderId}", request.OrderId);
            return new SuccessResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama UploadMultipleInvoiceLink exception. OrderId: {OrderId}", request.OrderId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
