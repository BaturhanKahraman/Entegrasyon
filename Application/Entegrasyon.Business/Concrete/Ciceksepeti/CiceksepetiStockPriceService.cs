using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti stok ve fiyat güncelleme servisi.
/// Max 200 item per batch. listPrice kullanmak için salesPrice zorunlu.
/// </summary>
public sealed class CiceksepetiStockPriceService(
    ICiceksepetiApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<CiceksepetiStockPriceService> logger) : ICiceksepetiStockPriceService
{
    private const int MaxBatchSize = 200;
    private const string Endpoint = "Products/price-and-stock";

    public async Task<IDataResult<List<string>>> UpdateStockAndPriceAsync(
        List<CiceksepetiStockPriceItem> items,
        CancellationToken ct = default)
    {
        // ── Validation ────────────────────────────────────────────────────────
        if (items == null || items.Count == 0)
        {
            const string emptyMsg = "Güncellenecek ürün listesi boş olamaz.";
            logger.LogWarning("CiceksepetiStockPriceService: {Message}", emptyMsg);
            return new ErrorDataResult<List<string>>(new List<string>(), emptyMsg);
        }

        var invalidItems = items
            .Where(x => x.ListPrice.HasValue && !x.SalesPrice.HasValue)
            .Select(x => x.StockCode)
            .ToList();

        if (invalidItems.Count > 0)
        {
            var errorMsg = $"ListPrice gönderildiğinde SalesPrice zorunludur. Geçersiz ürünler: {string.Join(", ", invalidItems)}";
            logger.LogWarning("CiceksepetiStockPriceService: {Message}", errorMsg);
            return new ErrorDataResult<List<string>>(new List<string>(), errorMsg);
        }

        // ── Business Rules ────────────────────────────────────────────────────
        var itemsWithNoUpdate = items
            .Where(x => !x.StockQuantity.HasValue && !x.SalesPrice.HasValue)
            .Select(x => x.StockCode)
            .ToList();

        if (itemsWithNoUpdate.Count > 0)
        {
            var errorMsg = $"Her item için en az StockQuantity veya SalesPrice gönderilmelidir. Eksik ürünler: {string.Join(", ", itemsWithNoUpdate)}";
            logger.LogWarning("CiceksepetiStockPriceService: {Message}", errorMsg);
            return new ErrorDataResult<List<string>>(new List<string>(), errorMsg);
        }

        // ── Execution ─────────────────────────────────────────────────────────
        var batchIds = new List<string>();
        var batches = SplitIntoBatches(items, MaxBatchSize);

        logger.LogInformation(
            "CiceksepetiStockPriceService: {ItemCount} ürün {BatchCount} batch ile gönderiliyor.",
            items.Count, batches.Count);

        foreach (var (batch, index) in batches.Select((b, i) => (b, i + 1)))
        {
            var request = new CiceksepetiStockPriceUpdateRequest(batch);

            try
            {
                var response = await apiClient.PutAsync(Endpoint, request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    var errorMsg = $"Batch {index} başarısız. HTTP {(int)response.StatusCode}: {body}";
                    logger.LogError("CiceksepetiStockPriceService: {Message}", errorMsg);
                    await applicationLogManager.AddLog(
                        errorMsg, LogType.StockSync, LogAction.Update, new { batch }, ct);
                    return new ErrorDataResult<List<string>>(batchIds, errorMsg);
                }

                var parsed = await response.Content.ReadFromJsonAsync<CiceksepetiBatchResponse>(
                    cancellationToken: ct);

                if (parsed is null)
                {
                    const string parseError = "API yanıtı batchId içermiyor.";
                    logger.LogError("CiceksepetiStockPriceService: {Message}", parseError);
                    return new ErrorDataResult<List<string>>(batchIds, parseError);
                }

                batchIds.Add(parsed.BatchId);

                logger.LogInformation(
                    "CiceksepetiStockPriceService: Batch {Index}/{Total} gönderildi. BatchId: {BatchId}",
                    index, batches.Count, parsed.BatchId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CiceksepetiStockPriceService: Batch {Index} gönderilemedi.", index);
                await applicationLogManager.AddLog(
                    $"Çiçeksepeti stok/fiyat güncellemesi başarısız: {ex.Message}",
                    LogType.StockSync, LogAction.Update, new { batch }, ct);
                return new ErrorDataResult<List<string>>(batchIds, ex.Message);
            }
        }

        await applicationLogManager.AddLog(
            $"Çiçeksepeti stok/fiyat güncellendi. {items.Count} ürün, {batchIds.Count} batch.",
            LogType.StockSync, LogAction.Update, new { batchIds }, ct);

        return new SuccessDataResult<List<string>>(batchIds,
            $"{items.Count} ürün başarıyla güncellendi. BatchId'ler: {string.Join(", ", batchIds)}");
    }

    private static List<List<CiceksepetiStockPriceItem>> SplitIntoBatches(
        List<CiceksepetiStockPriceItem> items,
        int batchSize)
    {
        var batches = new List<List<CiceksepetiStockPriceItem>>();
        for (var i = 0; i < items.Count; i += batchSize)
        {
            batches.Add(items.GetRange(i, Math.Min(batchSize, items.Count - i)));
        }
        return batches;
    }
}
