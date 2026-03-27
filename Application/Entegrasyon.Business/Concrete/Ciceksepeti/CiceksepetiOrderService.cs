using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti sipariş yönetim servisi.
/// Sipariş listeleme, kargo hazırlama ve kargo işlemlerini kapsar.
/// </summary>
public sealed class CiceksepetiOrderService(
    ICiceksepetiApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<CiceksepetiOrderService> logger) : ICiceksepetiOrderService
{
    private const string GetOrdersEndpoint = "Order/GetOrders";
    private const string CsCargoEndpoint = "Order/readyforcargowithcsintegration";
    private const string OwnCargoEndpoint = "Order/statusupdatewithsupplierintegration";
    private const string ChangeCargoEndpoint = "Order/CargoCompany";
    private const string CargoMeasurementEndpoint = "Order/CargoMeasurement";
    private const string DigitalCodeEndpoint = "Order/digital-order-status-update";
    private const string LaborCostEndpoint = "Order/UpdateLaborCost";

    public async Task<IDataResult<CiceksepetiOrderListResponse>> GetOrdersAsync(
        CiceksepetiGetOrdersRequest request,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation(
                "CiceksepetiOrderService: Sipariş listesi isteniyor. Page={Page}, PageSize={PageSize}",
                request.Page, request.PageSize);

            var response = await apiClient.PostAsync(GetOrdersEndpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"Sipariş listesi alınamadı. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiOrderService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.List, new { request }, ct);
                return new ErrorDataResult<CiceksepetiOrderListResponse>(null!, errorMsg);
            }

            var parsed = await response.Content.ReadFromJsonAsync<CiceksepetiOrderListResponse>(
                cancellationToken: ct);

            if (parsed is null)
            {
                const string parseError = "API yanıtı sipariş listesi içermiyor.";
                logger.LogError("CiceksepetiOrderService: {Message}", parseError);
                return new ErrorDataResult<CiceksepetiOrderListResponse>(null!, parseError);
            }

            logger.LogInformation(
                "CiceksepetiOrderService: {Count} sipariş alındı.",
                parsed.OrderListCount);

            return new SuccessDataResult<CiceksepetiOrderListResponse>(parsed,
                $"{parsed.OrderListCount} sipariş başarıyla alındı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiOrderService: Sipariş listesi alınamadı.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti sipariş listesi başarısız: {ex.Message}",
                LogType.Order, LogAction.List, new { request }, ct);
            return new ErrorDataResult<CiceksepetiOrderListResponse>(null!, ex.Message);
        }
    }

    public async Task<IResult> ReadyForCargoWithCsAsync(
        CiceksepetiCsCargoRequest request,
        CancellationToken ct = default)
    {
        return await ExecutePutAsync(CsCargoEndpoint, request, "CS kargo hazırlama", ct);
    }

    public async Task<IResult> UpdateStatusWithOwnCargoAsync(
        CiceksepetiOwnCargoRequest request,
        CancellationToken ct = default)
    {
        return await ExecutePutAsync(OwnCargoEndpoint, request, "Kendi kargo güncelleme", ct);
    }

    public async Task<IResult> ChangeCargoCompanyAsync(
        CiceksepetiChangeCargoRequest request,
        CancellationToken ct = default)
    {
        return await ExecutePutAsync(ChangeCargoEndpoint, request, "Kargo şirketi değiştirme", ct);
    }

    public async Task<IResult> SendCargoMeasurementAsync(
        CiceksepetiCargoMeasurementRequest request,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("CiceksepetiOrderService: Kargo ölçüsü gönderiliyor.");

            var response = await apiClient.PostAsync(CargoMeasurementEndpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"Kargo ölçüsü gönderilemedi. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiOrderService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.Update, new { request }, ct);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("CiceksepetiOrderService: Kargo ölçüsü başarıyla gönderildi.");
            return new SuccessResult("Kargo ölçüsü başarıyla gönderildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiOrderService: Kargo ölçüsü gönderilemedi.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti kargo ölçüsü başarısız: {ex.Message}",
                LogType.Order, LogAction.Update, new { request }, ct);
            return new ErrorResult(ex.Message);
        }
    }

    public async Task<IResult> SendDigitalCodeAsync(
        CiceksepetiDigitalCodeRequest request,
        CancellationToken ct = default)
    {
        return await ExecutePutAsync(DigitalCodeEndpoint, request, "Dijital kod gönderme", ct);
    }

    public async Task<IResult> UpdateLaborCostAsync(
        CiceksepetiLaborCostRequest request,
        CancellationToken ct = default)
    {
        return await ExecutePutAsync(LaborCostEndpoint, request, "İşçilik maliyeti güncelleme", ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<IResult> ExecutePutAsync<T>(
        string endpoint,
        T request,
        string operationName,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation("CiceksepetiOrderService: {Operation} başlatılıyor.", operationName);

            var response = await apiClient.PutAsync(endpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"{operationName} başarısız. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiOrderService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.Update, new { endpoint, request }, ct);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("CiceksepetiOrderService: {Operation} başarıyla tamamlandı.", operationName);
            return new SuccessResult($"{operationName} başarıyla tamamlandı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiOrderService: {Operation} başarısız.", operationName);
            await applicationLogManager.AddLog(
                $"Çiçeksepeti {operationName} başarısız: {ex.Message}",
                LogType.Order, LogAction.Update, new { endpoint, request }, ct);
            return new ErrorResult(ex.Message);
        }
    }
}
