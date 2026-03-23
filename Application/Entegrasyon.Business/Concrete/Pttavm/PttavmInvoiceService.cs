using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// PttAVM fatura gonderim servisi.
/// integration-api.pttavm.com uzerinde calisir (CatalogApiClient kullanir).
/// </summary>
public sealed class PttavmInvoiceService(
    IPttavmCatalogApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<PttavmInvoiceService> logger) : IPttavmInvoiceService
{
    public async Task<IResult> SendInvoiceAsync(
        string orderId, List<int> lineItemIds, string? pdfUrl, string? base64Content,
        CancellationToken ct = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(orderId))
        {
            const string msg = "Sipariş numarası boş olamaz.";
            logger.LogWarning("PttavmInvoiceService: {Message}", msg);
            return new ErrorResult(msg);
        }

        if (lineItemIds is null || lineItemIds.Count == 0)
        {
            const string msg = "Fatura kalem listesi boş olamaz.";
            logger.LogWarning("PttavmInvoiceService: {Message}", msg);
            return new ErrorResult(msg);
        }

        // Execution
        try
        {
            var request = new PttavmInvoiceRequest(lineItemIds, base64Content, pdfUrl);
            var response = await apiClient.PostAsync($"api/v1/orders/{orderId}/invoice", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("PttAVM invoice send failed: {Status} {Body}", response.StatusCode, errorBody);
                await applicationLogManager.AddLog(
                    $"PttAVM fatura gönderimi başarısız: {response.StatusCode}",
                    LogType.Invoice, LogAction.Update, null, ct);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmInvoiceResult>(cancellationToken: ct);
            if (result is not null && !result.Success)
            {
                var errorMsg = result.ErrorMessage ?? "Fatura gönderilemedi.";
                logger.LogWarning("PttAVM invoice send failed: {Message}", errorMsg);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("PttAVM invoice sent: orderId={OrderId}, {Count} items", orderId, lineItemIds.Count);
            await applicationLogManager.AddLog(
                $"PttAVM fatura gönderildi. Sipariş: {orderId}",
                LogType.Invoice, LogAction.Update, null, ct);

            return new SuccessResult("Fatura başarıyla gönderildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM invoice send exception for {OrderId}", orderId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
