using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// PttAVM urun publish servisi.
/// Upsert, tracking, barkod kontrol, aktif/pasif, hatali gorseller.
/// </summary>
public sealed class PttavmProductService(
    IPttavmCatalogApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<PttavmProductService> logger) : IPttavmProductService
{
    private const int MaxBatchSize = 1000;

    // ── UpsertProductsAsync ─────────────────────────────────────────────────

    public async Task<IDataResult<PttavmUpsertResult>> UpsertProductsAsync(
        List<PttavmProductRequest> products, CancellationToken ct = default)
    {
        // Validation
        if (products is null || products.Count == 0)
        {
            const string msg = "Gönderilecek ürün listesi boş olamaz.";
            logger.LogWarning("PttavmProductService: {Message}", msg);
            return new ErrorDataResult<PttavmUpsertResult>(null, msg);
        }

        if (products.Count > MaxBatchSize)
        {
            var msg = $"Maksimum {MaxBatchSize} ürün/istek gönderilebilir. Gönderilen: {products.Count}";
            logger.LogWarning("PttavmProductService: {Message}", msg);
            return new ErrorDataResult<PttavmUpsertResult>(null, msg);
        }

        // Execution
        try
        {
            var response = await apiClient.PostAsync("api/v1/products/upsert", products);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("PttAVM upsert failed: {Status} {Body}", response.StatusCode, errorBody);
                await applicationLogManager.AddLog(
                    $"PttAVM ürün gönderimi başarısız: {response.StatusCode}",
                    LogType.Product, LogAction.None, null, ct);
                return new ErrorDataResult<PttavmUpsertResult>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmUpsertResult>(cancellationToken: ct);
            if (result is null)
            {
                const string msg = "PttAVM'den upsert yanıtı alınamadı.";
                logger.LogWarning(msg);
                return new ErrorDataResult<PttavmUpsertResult>(null, msg);
            }

            logger.LogInformation("PttAVM upsert success: {Count} products, trackingId={TrackingId}",
                products.Count, result.TrackingId);
            await applicationLogManager.AddLog(
                $"PttAVM ürün gönderildi. TrackingId: {result.TrackingId}",
                LogType.Product, LogAction.None, null, ct);

            return new SuccessDataResult<PttavmUpsertResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM upsert exception");
            return new ErrorDataResult<PttavmUpsertResult>(null, $"Hata: {ex.Message}");
        }
    }

    // ── GetTrackingResultAsync ───────────────────────────────────────────────

    public async Task<IDataResult<PttavmTrackingResult>> GetTrackingResultAsync(
        string trackingId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingId))
        {
            const string msg = "TrackingId boş olamaz.";
            logger.LogWarning("PttavmProductService: {Message}", msg);
            return new ErrorDataResult<PttavmTrackingResult>(null, msg);
        }

        try
        {
            var response = await apiClient.PostAsync($"api/v1/products/tracking-result/{trackingId}", new { });

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM tracking check failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PttavmTrackingResult>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmTrackingResult>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<PttavmTrackingResult>(null, "Tracking bilgisi alınamadı.");

            logger.LogInformation("PttAVM tracking: {TrackingId}, status={Status}", trackingId, result.Status);
            return new SuccessDataResult<PttavmTrackingResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM tracking exception for {TrackingId}", trackingId);
            return new ErrorDataResult<PttavmTrackingResult>(null, $"Hata: {ex.Message}");
        }
    }

    // ── GetProductByBarcodeAsync ─────────────────────────────────────────────

    public async Task<IDataResult<PttavmProductInfo>> GetProductByBarcodeAsync(
        string barcode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            const string msg = "Barkod boş olamaz.";
            logger.LogWarning("PttavmProductService: {Message}", msg);
            return new ErrorDataResult<PttavmProductInfo>(null, msg);
        }

        try
        {
            var response = await apiClient.GetAsync($"api/v1/products?barcode={barcode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM get product failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PttavmProductInfo>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmProductInfo>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<PttavmProductInfo>(null, "Ürün bilgisi alınamadı.");

            return new SuccessDataResult<PttavmProductInfo>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM get product exception for barcode {Barcode}", barcode);
            return new ErrorDataResult<PttavmProductInfo>(null, $"Hata: {ex.Message}");
        }
    }

    // ── GetProductsByBarcodesAsync ───────────────────────────────────────────

    public async Task<IDataResult<List<PttavmProductInfo>>> GetProductsByBarcodesAsync(
        List<string> barcodes, CancellationToken ct = default)
    {
        if (barcodes is null || barcodes.Count == 0)
        {
            const string msg = "Barkod listesi boş olamaz.";
            logger.LogWarning("PttavmProductService: {Message}", msg);
            return new ErrorDataResult<List<PttavmProductInfo>>(null, msg);
        }

        try
        {
            var request = new PttavmGetByBarcodesRequest(barcodes);
            var response = await apiClient.PostAsync("api/v1/products/get-by-barcodes", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM get products by barcodes failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PttavmProductInfo>>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<PttavmProductInfo>>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<List<PttavmProductInfo>>(null, "Ürün listesi alınamadı.");

            logger.LogInformation("PttAVM get products by barcodes: {Count} products returned", result.Count);
            return new SuccessDataResult<List<PttavmProductInfo>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM get products by barcodes exception");
            return new ErrorDataResult<List<PttavmProductInfo>>(null, $"Hata: {ex.Message}");
        }
    }

    // ── SetProductStatusAsync ───────────────────────────────────────────────

    public async Task<IResult> SetProductStatusAsync(
        int productId, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var request = new PttavmProductStatusRequest(isActive);
            var response = await apiClient.PutAsync($"api/v1/products/{productId}/status", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM set status failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            logger.LogInformation("PttAVM product {ProductId} status set to {IsActive}", productId, isActive);
            return new SuccessResult($"Ürün durumu güncellendi: {(isActive ? "Aktif" : "Pasif")}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM set status exception for {ProductId}", productId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    // ── GetFaultyImagesAsync ────────────────────────────────────────────────

    public async Task<IDataResult<PttavmFaultyImagesResult>> GetFaultyImagesAsync(
        List<string>? barcodes, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var request = new PttavmFaultyImagesRequest(
                barcodes,
                new PttavmPaginationParameters(page, pageSize));

            var response = await apiClient.PostAsync("api/v1/products/get-faulty-images", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM faulty images failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PttavmFaultyImagesResult>(null, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmFaultyImagesResult>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<PttavmFaultyImagesResult>(null, "Hatalı görsel bilgisi alınamadı.");

            return new SuccessDataResult<PttavmFaultyImagesResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM faulty images exception");
            return new ErrorDataResult<PttavmFaultyImagesResult>(null, $"Hata: {ex.Message}");
        }
    }
}
