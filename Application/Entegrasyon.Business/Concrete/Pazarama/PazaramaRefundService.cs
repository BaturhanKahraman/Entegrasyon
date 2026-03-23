using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama iade ve iptal servisi (gerçek API çağrıları).
/// GetRefunds: POST /order/getRefund
/// UpdateRefund: POST /order/updateRefund
/// GetCancellations: POST /order/api/cancel/items
/// UpdateCancellation: PUT /order/api/cancel
/// </summary>
public sealed class PazaramaRefundService(
    IPazaramaApiClient apiClient,
    ILogger<PazaramaRefundService> logger) : IPazaramaRefundService
{
    public async Task<IDataResult<PazaramaRefundListResponse>> GetRefundsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1)
    {
        try
        {
            var request = new PazaramaRefundFetchRequest(
                PageSize: pageSize,
                PageNumber: pageNumber,
                RefundStatus: refundStatus,
                RequestStartDate: startDate.ToString("yyyy-MM-dd"),
                RequestEndDate: endDate.ToString("yyyy-MM-dd"));

            var response = await apiClient.PostAsync("order/getRefund", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama GetRefunds başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PazaramaRefundListResponse>(null, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<PazaramaRefundListResponse>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "İadeler alınamadı";
                logger.LogWarning("Pazarama GetRefunds yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<PazaramaRefundListResponse>(null, msg);
            }

            logger.LogInformation("Pazarama GetRefunds başarılı. İade sayısı: {Count}",
                parsed.Data?.RefundList?.Count ?? 0);
            return new SuccessDataResult<PazaramaRefundListResponse>(parsed.Data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama GetRefunds exception. StartDate: {Start}, EndDate: {End}", startDate, endDate);
            return new ErrorDataResult<PazaramaRefundListResponse>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> UpdateRefundAsync(string refundId, int status, int? refundRejectType = null)
    {
        try
        {
            var request = new PazaramaRefundUpdateRequest(
                RefundId: refundId,
                Status: status,
                RefundRejectType: refundRejectType);

            var response = await apiClient.PostAsync("order/updateRefund", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama UpdateRefund başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<object>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "İade güncellenemedi";
                logger.LogWarning("Pazarama UpdateRefund yanıt başarısız: {Message}", msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("Pazarama UpdateRefund başarılı. RefundId: {RefundId}, Status: {Status}",
                refundId, status);
            return new SuccessResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama UpdateRefund exception. RefundId: {RefundId}", refundId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<PazaramaRefundListResponse>> GetCancellationsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1)
    {
        try
        {
            var request = new PazaramaRefundFetchRequest(
                PageSize: pageSize,
                PageNumber: pageNumber,
                RefundStatus: refundStatus,
                RequestStartDate: startDate.ToString("yyyy-MM-dd"),
                RequestEndDate: endDate.ToString("yyyy-MM-dd"));

            var response = await apiClient.PostAsync("order/api/cancel/items", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama GetCancellations başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PazaramaRefundListResponse>(null, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<PazaramaRefundListResponse>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "İptaller alınamadı";
                logger.LogWarning("Pazarama GetCancellations yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<PazaramaRefundListResponse>(null, msg);
            }

            logger.LogInformation("Pazarama GetCancellations başarılı. İptal sayısı: {Count}",
                parsed.Data?.RefundList?.Count ?? 0);
            return new SuccessDataResult<PazaramaRefundListResponse>(parsed.Data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama GetCancellations exception. StartDate: {Start}, EndDate: {End}", startDate, endDate);
            return new ErrorDataResult<PazaramaRefundListResponse>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> UpdateCancellationAsync(string refundId, int status)
    {
        try
        {
            var request = new PazaramaCancelUpdateRequest(
                RefundId: refundId,
                Status: status);

            var response = await apiClient.PutAsync("order/api/cancel", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama UpdateCancellation başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<object>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "İptal güncellenemedi";
                logger.LogWarning("Pazarama UpdateCancellation yanıt başarısız: {Message}", msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("Pazarama UpdateCancellation başarılı. RefundId: {RefundId}, Status: {Status}",
                refundId, status);
            return new SuccessResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama UpdateCancellation exception. RefundId: {RefundId}", refundId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
