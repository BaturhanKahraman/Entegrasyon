using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// PttAVM stok ve fiyat guncelleme servisi.
/// Max 1000 item per batch. Stok 0-9999, indirim 0-70.
/// </summary>
public sealed class PttavmStockPriceService(
    IPttavmCatalogApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<PttavmStockPriceService> logger) : IPttavmStockPriceService
{
    private const int MaxBatchSize = 1000;
    private const int MaxStock = 9999;
    private const decimal MaxDiscount = 70;

    // ── UpdateStockPricesAsync ───────────────────────────────────────────────

    public async Task<IDataResult<PttavmUpsertResult>> UpdateStockPricesAsync(
        List<PttavmStockPriceRequest> items, CancellationToken ct = default)
    {
        // ── Validation ──────────────────────────────────────────────────────
        if (items is null || items.Count == 0)
        {
            const string msg = "Güncellenecek ürün listesi boş olamaz.";
            logger.LogWarning("PttavmStockPriceService: {Message}", msg);
            return new ErrorDataResult<PttavmUpsertResult>(null!, msg);
        }

        if (items.Count > MaxBatchSize)
        {
            var msg = $"Maksimum {MaxBatchSize} ürün/istek gönderilebilir. Gönderilen: {items.Count}";
            logger.LogWarning("PttavmStockPriceService: {Message}", msg);
            return new ErrorDataResult<PttavmUpsertResult>(null!, msg);
        }

        // ── Business Rules ──────────────────────────────────────────────────
        var invalidStockItems = items
            .Where(x => x.Quantity.HasValue && (x.Quantity.Value < 0 || x.Quantity.Value > MaxStock))
            .Select(x => x.Barcode)
            .ToList();

        if (invalidStockItems.Count > 0)
        {
            var msg = $"Stok değeri 0-{MaxStock} aralığında olmalıdır. Geçersiz barkodlar: {string.Join(", ", invalidStockItems)}";
            logger.LogWarning("PttavmStockPriceService: {Message}", msg);
            return new ErrorDataResult<PttavmUpsertResult>(null!, msg);
        }

        var invalidDiscountItems = items
            .Where(x => x.Discount.HasValue && (x.Discount.Value < 0 || x.Discount.Value > MaxDiscount))
            .Select(x => x.Barcode)
            .ToList();

        if (invalidDiscountItems.Count > 0)
        {
            var msg = $"İndirim değeri 0-{MaxDiscount} aralığında olmalıdır. Geçersiz barkodlar: {string.Join(", ", invalidDiscountItems)}";
            logger.LogWarning("PttavmStockPriceService: {Message}", msg);
            return new ErrorDataResult<PttavmUpsertResult>(null!, msg);
        }

        // ── Execution ───────────────────────────────────────────────────────
        try
        {
            var response = await apiClient.PostAsync("api/v1/products/stock-prices", items);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("PttAVM stock-prices update failed: {Status} {Body}", response.StatusCode, errorBody);
                await applicationLogManager.AddLog(
                    $"PttAVM stok/fiyat güncellemesi başarısız: {response.StatusCode}",
                    LogType.StockSync, LogAction.Update, null, ct);
                return new ErrorDataResult<PttavmUpsertResult>(null!, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmUpsertResult>(cancellationToken: ct);
            if (result is null)
            {
                const string msg = "PttAVM'den stok/fiyat yanıtı alınamadı.";
                logger.LogWarning(msg);
                return new ErrorDataResult<PttavmUpsertResult>(null!, msg);
            }

            logger.LogInformation("PttAVM stock-prices update success: {Count} items, trackingId={TrackingId}",
                items.Count, result.TrackingId);
            await applicationLogManager.AddLog(
                $"PttAVM stok/fiyat güncellendi. {items.Count} ürün, TrackingId: {result.TrackingId}",
                LogType.StockSync, LogAction.Update, null, ct);

            return new SuccessDataResult<PttavmUpsertResult>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM stock-prices update exception");
            return new ErrorDataResult<PttavmUpsertResult>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── SearchProductsAsync ─────────────────────────────────────────────────

    public async Task<IDataResult<List<PttavmProductInfo>>> SearchProductsAsync(
        PttavmProductSearchFilter filter, CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (filter.CategoryId.HasValue) queryParams.Add($"categoryId={filter.CategoryId}");
            if (filter.SubCategoryId.HasValue) queryParams.Add($"subCategoryId={filter.SubCategoryId}");
            if (filter.IsActive.HasValue) queryParams.Add($"isActive={filter.IsActive.Value.ToString().ToLower()}");
            if (filter.IsInStock.HasValue) queryParams.Add($"isInStock={filter.IsInStock.Value.ToString().ToLower()}");
            if (filter.MerchantCategoryId.HasValue) queryParams.Add($"merchantCategoryId={filter.MerchantCategoryId}");
            queryParams.Add($"searchPage={filter.SearchPage}");

            var url = "api/v1/products/search";
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await apiClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM product search failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PttavmProductInfo>>(null!, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<PttavmProductInfo>>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<List<PttavmProductInfo>>(null!, "Ürün arama sonucu alınamadı.");

            logger.LogInformation("PttAVM product search: {Count} products found", result.Count);
            return new SuccessDataResult<List<PttavmProductInfo>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM product search exception");
            return new ErrorDataResult<List<PttavmProductInfo>>(null!, $"Hata: {ex.Message}");
        }
    }
}
