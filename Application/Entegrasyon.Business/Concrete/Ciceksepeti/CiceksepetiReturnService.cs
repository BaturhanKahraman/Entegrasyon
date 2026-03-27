using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti iade yönetim servisi.
/// İade listesi, teslim alındı bildirimi ve değerlendirme işlemlerini kapsar.
/// </summary>
public sealed class CiceksepetiReturnService(
    ICiceksepetiApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<CiceksepetiReturnService> logger) : ICiceksepetiReturnService
{
    private const string GetReturnsEndpoint = "Order/getcanceledorders";
    private const string ConfirmReceivedEndpoint = "Order/refundprocessstartreceivedprocess";
    private const string EvaluateReturnEndpoint = "Order/cancelevaluation";

    public async Task<IDataResult<CiceksepetiReturnListResponse>> GetReturnOrdersAsync(
        CiceksepetiGetReturnsRequest request,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation(
                "CiceksepetiReturnService: İade listesi isteniyor. Page={Page}, PageSize={PageSize}",
                request.Page, request.PageSize);

            var response = await apiClient.PostAsync(GetReturnsEndpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"İade listesi alınamadı. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiReturnService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.List, new { request }, ct);
                return new ErrorDataResult<CiceksepetiReturnListResponse>(null!, errorMsg);
            }

            var parsed = await response.Content.ReadFromJsonAsync<CiceksepetiReturnListResponse>(
                cancellationToken: ct);

            if (parsed is null)
            {
                const string parseError = "API yanıtı iade listesi içermiyor.";
                logger.LogError("CiceksepetiReturnService: {Message}", parseError);
                return new ErrorDataResult<CiceksepetiReturnListResponse>(null!, parseError);
            }

            logger.LogInformation(
                "CiceksepetiReturnService: {Count} iade alındı.",
                parsed.OrderItemList.Count);

            return new SuccessDataResult<CiceksepetiReturnListResponse>(parsed,
                $"{parsed.OrderItemList.Count} iade başarıyla alındı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiReturnService: İade listesi alınamadı.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti iade listesi başarısız: {ex.Message}",
                LogType.Order, LogAction.List, new { request }, ct);
            return new ErrorDataResult<CiceksepetiReturnListResponse>(null!, ex.Message);
        }
    }

    public async Task<IResult> ConfirmReturnReceivedAsync(
        CiceksepetiReturnReceivedRequest request,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation(
                "CiceksepetiReturnService: İade teslim alındı bildirimi gönderiliyor. {Count} kalem.",
                request.OrderItemIds.Count);

            var response = await apiClient.PostAsync(ConfirmReceivedEndpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"İade teslim alındı bildirimi gönderilemedi. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiReturnService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.Update, new { request }, ct);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("CiceksepetiReturnService: İade teslim alındı bildirimi başarıyla gönderildi.");
            return new SuccessResult("İade teslim alındı bildirimi başarıyla gönderildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiReturnService: İade teslim alındı bildirimi gönderilemedi.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti iade teslim alındı bildirimi başarısız: {ex.Message}",
                LogType.Order, LogAction.Update, new { request }, ct);
            return new ErrorResult(ex.Message);
        }
    }

    public async Task<IResult> EvaluateReturnAsync(
        CiceksepetiReturnEvaluationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation(
                "CiceksepetiReturnService: İade değerlendirmesi yapılıyor. OrderItemId={OrderItemId}, Process={Process}",
                request.OrderItemId, request.Process);

            var response = await apiClient.PostAsync(EvaluateReturnEndpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"İade değerlendirmesi yapılamadı. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiReturnService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Order, LogAction.Update, new { request }, ct);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("CiceksepetiReturnService: İade değerlendirmesi başarıyla yapıldı.");
            return new SuccessResult("İade değerlendirmesi başarıyla yapıldı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiReturnService: İade değerlendirmesi yapılamadı.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti iade değerlendirmesi başarısız: {ex.Message}",
                LogType.Order, LogAction.Update, new { request }, ct);
            return new ErrorResult(ex.Message);
        }
    }
}
