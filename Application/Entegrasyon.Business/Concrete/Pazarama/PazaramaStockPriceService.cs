using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama stok ve fiyat güncelleme servisi (gerçek API çağrıları).
/// Stock: POST /product/updateStock-v2
/// Price: POST /product/updatePrice-v2
/// Her ikisi de PazaramaResponse&lt;string&gt; döner; data alanı batch takibi için dataId'dir.
/// </summary>
public sealed class PazaramaStockPriceService(
    IPazaramaApiClient apiClient,
    ILogger<PazaramaStockPriceService> logger) : IPazaramaStockPriceService
{
    public async Task<IDataResult<string>> UpdateStockAsync(List<PazaramaStockUpdateItem> items)
    {
        try
        {
            var request = new PazaramaStockUpdateRequest(items);
            var response = await apiClient.PostAsync("product/updateStock-v2", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama UpdateStock failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<string>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<string>>();

            if (parsed?.Success != true || parsed.Data is null)
            {
                var msg = parsed?.Message ?? "dataId alınamadı";
                logger.LogWarning("Pazarama UpdateStock yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<string>(null!, msg);
            }

            logger.LogInformation("Pazarama UpdateStock başarılı. DataId: {DataId}, İşlem sayısı: {Count}",
                parsed.Data, items.Count);

            return new SuccessDataResult<string>(parsed.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama UpdateStock exception. İşlem sayısı: {Count}", items.Count);
            return new ErrorDataResult<string>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<string>> UpdatePriceAsync(List<PazaramaPriceUpdateItem> items)
    {
        try
        {
            var request = new PazaramaPriceUpdateRequest(items);
            var response = await apiClient.PostAsync("product/updatePrice-v2", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama UpdatePrice failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<string>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<string>>();

            if (parsed?.Success != true || parsed.Data is null)
            {
                var msg = parsed?.Message ?? "dataId alınamadı";
                logger.LogWarning("Pazarama UpdatePrice yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<string>(null!, msg);
            }

            logger.LogInformation("Pazarama UpdatePrice başarılı. DataId: {DataId}, İşlem sayısı: {Count}",
                parsed.Data, items.Count);

            return new SuccessDataResult<string>(parsed.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama UpdatePrice exception. İşlem sayısı: {Count}", items.Count);
            return new ErrorDataResult<string>(null!, $"Hata: {ex.Message}");
        }
    }
}
