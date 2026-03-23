using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// PttAVM kargo islemleri servisi.
/// Depo listeleme, barkod olusturma, etiket alma, dijital urun teslimi.
/// shipment.pttavm.com uzerinde calisir (ShipmentApiClient kullanir).
/// </summary>
public sealed class PttavmShippingService(
    IPttavmShipmentApiClient shipmentClient,
    IApplicationLogManager applicationLogManager,
    ILogger<PttavmShippingService> logger) : IPttavmShippingService
{
    // ── GetWarehousesAsync ──────────────────────────────────────────────────

    public async Task<IDataResult<List<PttavmWarehouse>>> GetWarehousesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await shipmentClient.PostAsync("api/v1/get-warehouse", new { });

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM get warehouses failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PttavmWarehouse>>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<PttavmWarehouse>>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<List<PttavmWarehouse>>(null, "Depo listesi alınamadı.");

            logger.LogInformation("PttAVM warehouses: {Count} warehouses found", result.Count);
            return new SuccessDataResult<List<PttavmWarehouse>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM get warehouses exception");
            return new ErrorDataResult<List<PttavmWarehouse>>(null, $"Hata: {ex.Message}");
        }
    }

    // ── CreateBarcodesAsync ─────────────────────────────────────────────────

    public async Task<IDataResult<PttavmBarcodeCreateResult>> CreateBarcodesAsync(
        List<PttavmBarcodeRequest> orders, CancellationToken ct = default)
    {
        if (orders is null || orders.Count == 0)
        {
            const string msg = "Sipariş listesi boş olamaz.";
            logger.LogWarning("PttavmShippingService: {Message}", msg);
            return new ErrorDataResult<PttavmBarcodeCreateResult>(null, msg);
        }

        try
        {
            var request = new PttavmBarcodeCreateRequestBody(orders);
            var response = await shipmentClient.PostAsync("api/v1/create-barcode", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM create barcodes failed: {Status} {Body}", response.StatusCode, errorBody);
                await applicationLogManager.AddLog(
                    $"PttAVM barkod oluşturma başarısız: {response.StatusCode}",
                    LogType.Order, LogAction.Update, null, ct);
                return new ErrorDataResult<PttavmBarcodeCreateResult>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmBarcodeCreateResult>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<PttavmBarcodeCreateResult>(null, "Barkod oluşturma yanıtı alınamadı.");

            logger.LogInformation("PttAVM create barcodes: trackingId={TrackingId}, count={Count}",
                result.TrackingId, result.Count);
            return new SuccessDataResult<PttavmBarcodeCreateResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM create barcodes exception");
            return new ErrorDataResult<PttavmBarcodeCreateResult>(null, $"Hata: {ex.Message}");
        }
    }

    // ── CheckBarcodeStatusAsync ─────────────────────────────────────────────

    public async Task<IDataResult<PttavmBarcodeStatusResult>> CheckBarcodeStatusAsync(
        string trackingId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingId))
        {
            const string msg = "TrackingId boş olamaz.";
            logger.LogWarning("PttavmShippingService: {Message}", msg);
            return new ErrorDataResult<PttavmBarcodeStatusResult>(null, msg);
        }

        try
        {
            var request = new PttavmBarcodeStatusRequestBody(trackingId);
            var response = await shipmentClient.PostAsync("api/v1/barcode-status", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM barcode status failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PttavmBarcodeStatusResult>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmBarcodeStatusResult>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<PttavmBarcodeStatusResult>(null, "Barkod durumu alınamadı.");

            logger.LogInformation("PttAVM barcode status: trackingId={TrackingId}, status={Status}",
                trackingId, result.Status);
            return new SuccessDataResult<PttavmBarcodeStatusResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM barcode status exception for {TrackingId}", trackingId);
            return new ErrorDataResult<PttavmBarcodeStatusResult>(null, $"Hata: {ex.Message}");
        }
    }

    // ── GetBarcodeTagAsync ──────────────────────────────────────────────────

    public async Task<IDataResult<string>> GetBarcodeTagAsync(
        string barcode, string orderId, string? type = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            const string msg = "Barkod boş olamaz.";
            logger.LogWarning("PttavmShippingService: {Message}", msg);
            return new ErrorDataResult<string>(null, msg);
        }

        if (string.IsNullOrWhiteSpace(orderId))
        {
            const string msg = "Sipariş numarası boş olamaz.";
            logger.LogWarning("PttavmShippingService: {Message}", msg);
            return new ErrorDataResult<string>(null, msg);
        }

        try
        {
            var request = new PttavmBarcodeTagRequest(barcode, orderId, type);
            var response = await shipmentClient.PostAsync("api/v1/get-barcode-tag", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM barcode tag failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<string>(null, $"API hatası: {response.StatusCode}");
            }

            var tagContent = await response.Content.ReadAsStringAsync(ct);
            return new SuccessDataResult<string>(tagContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM barcode tag exception for {Barcode}", barcode);
            return new ErrorDataResult<string>(null, $"Hata: {ex.Message}");
        }
    }

    // ── UpdateNoShippingOrderAsync ──────────────────────────────────────────

    public async Task<IResult> UpdateNoShippingOrderAsync(
        string orderId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            const string msg = "Sipariş numarası boş olamaz.";
            logger.LogWarning("PttavmShippingService: {Message}", msg);
            return new ErrorResult(msg);
        }

        try
        {
            var request = new PttavmNoShippingOrderRequest(orderId);
            var response = await shipmentClient.PostAsync("api/v1/update-no-shipping-order", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM no-shipping update failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmNoShippingResult>(cancellationToken: ct);
            if (result is null || !result.Status)
            {
                var errorMsg = result?.Message ?? "Kargosuz sipariş güncellenemedi.";
                logger.LogWarning("PttAVM no-shipping update failed: {Message}", errorMsg);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("PttAVM no-shipping order updated: {OrderId}", orderId);
            return new SuccessResult("Kargosuz sipariş teslim edildi olarak güncellendi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM no-shipping update exception for {OrderId}", orderId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
