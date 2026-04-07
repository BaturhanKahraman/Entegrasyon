using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// PttAVM Sipariş yonetim servisi.
/// Sipariş arama, detay, kargo bilgi ve kargo profilleri.
/// integration-api.pttavm.com uzerinde calisir (CatalogApiClient kullanir).
/// </summary>
public sealed class PttavmOrderService(
    IPttavmCatalogApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<PttavmOrderService> logger) : IPttavmOrderService
{
    private const int MaxDateRangeDays = 40;

    // ── SearchOrdersAsync ───────────────────────────────────────────────────

    public async Task<IDataResult<List<PttavmOrder>>> SearchOrdersAsync(
        DateTime startDate, DateTime endDate, bool isActiveOrders, CancellationToken ct = default)
    {
        // Validation
        if (endDate < startDate)
        {
            const string msg = "Bitiş tarihi başlangıç tarihinden önce olamaz.";
            logger.LogWarning("PttavmOrderService: {Message}", msg);
            return new ErrorDataResult<List<PttavmOrder>>(null!, msg);
        }

        if ((endDate - startDate).TotalDays > MaxDateRangeDays)
        {
            var msg = $"Tarih aralığı maksimum {MaxDateRangeDays} gün olabilir.";
            logger.LogWarning("PttavmOrderService: {Message}", msg);
            return new ErrorDataResult<List<PttavmOrder>>(null!, msg);
        }

        // Execution
        try
        {
            var url = $"api/v1/orders/search?startDate={startDate:O}&endDate={endDate:O}&isActiveOrders={isActiveOrders.ToString().ToLower()}";
            var response = await apiClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM order search failed: {Status} {Body}", response.StatusCode, errorBody);
                await applicationLogManager.AddLog(
                    $"PttAVM sipariş arama başarısız: {response.StatusCode}",
                    LogType.Order, LogAction.List, null, ct);
                return new ErrorDataResult<List<PttavmOrder>>(null!, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<PttavmOrder>>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<List<PttavmOrder>>(null!, "Sipariş listesi alınamadı.");

            logger.LogInformation("PttAVM order search: {Count} orders found", result.Count);
            return new SuccessDataResult<List<PttavmOrder>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM order search exception");
            return new ErrorDataResult<List<PttavmOrder>>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── GetOrderDetailAsync ─────────────────────────────────────────────────

    public async Task<IDataResult<PttavmOrderDetail>> GetOrderDetailAsync(
        string orderId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            const string msg = "Sipariş numarası boş olamaz.";
            logger.LogWarning("PttavmOrderService: {Message}", msg);
            return new ErrorDataResult<PttavmOrderDetail>(null!, msg);
        }

        try
        {
            var response = await apiClient.GetAsync($"api/v1/orders/{orderId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM order detail failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PttavmOrderDetail>(null!, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmOrderDetail>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<PttavmOrderDetail>(null!, "Sipariş detayı alınamadı.");

            return new SuccessDataResult<PttavmOrderDetail>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM order detail exception for {OrderId}", orderId);
            return new ErrorDataResult<PttavmOrderDetail>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── GetCargoInfosAsync ──────────────────────────────────────────────────

    public async Task<IDataResult<List<PttavmCargoInfo>>> GetCargoInfosAsync(
        string orderId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            const string msg = "Sipariş numarası boş olamaz.";
            logger.LogWarning("PttavmOrderService: {Message}", msg);
            return new ErrorDataResult<List<PttavmCargoInfo>>(null!, msg);
        }

        try
        {
            var response = await apiClient.GetAsync($"api/v1/orders/{orderId}/cargo-infos");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM cargo infos failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PttavmCargoInfo>>(null!, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<PttavmCargoInfo>>(cancellationToken: ct);
            if (result is null)
                return new ErrorDataResult<List<PttavmCargoInfo>>(null!, "Kargo bilgileri alınamadı.");

            return new SuccessDataResult<List<PttavmCargoInfo>>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM cargo infos exception for {OrderId}", orderId);
            return new ErrorDataResult<List<PttavmCargoInfo>>(null!, $"Hata: {ex.Message}");
        }
    }

    // ── GetCargoProfilesAsync ───────────────────────────────────────────────

    public async Task<IDataResult<List<PttavmCargoProfile>>> GetCargoProfilesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.GetAsync("api/v1/shipping/cargo-profiles");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("PttAVM cargo profiles failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PttavmCargoProfile>>(null!, $"API hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PttavmCargoProfileResponse>(cancellationToken: ct);
            if (result?.CargoProfiles is null)
                return new ErrorDataResult<List<PttavmCargoProfile>>(null!, "Kargo profilleri alınamadı.");

            logger.LogInformation("PttAVM cargo profiles: {Count} profiles found", result.CargoProfiles.Count);
            return new SuccessDataResult<List<PttavmCargoProfile>>(result.CargoProfiles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM cargo profiles exception");
            return new ErrorDataResult<List<PttavmCargoProfile>>(null!, $"Hata: {ex.Message}");
        }
    }
}
