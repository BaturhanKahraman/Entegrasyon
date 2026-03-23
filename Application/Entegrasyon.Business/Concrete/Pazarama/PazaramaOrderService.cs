using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama sipariş servisi (gerçek API çağrıları).
/// Fetch: POST /order/getOrdersForApi
/// Update: PUT /order/updateOrderStatus
/// BulkUpdate: PUT /order/updateOrderStatusList
/// </summary>
public sealed class PazaramaOrderService(
    IPazaramaApiClient apiClient,
    ILogger<PazaramaOrderService> logger) : IPazaramaOrderService
{
    public async Task<IDataResult<List<PazaramaOrderDto>>> FetchOrdersAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int pageSize = 500, int pageNumber = 1)
    {
        try
        {
            var request = new PazaramaOrderFetchRequest(
                OrderNumber: null,
                StartDate: startDate.ToString("yyyy-MM-dd"),
                EndDate: endDate.ToString("yyyy-MM-dd"),
                PageSize: pageSize,
                PageNumber: pageNumber);

            var response = await apiClient.PostAsync("order/getOrdersForApi", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama FetchOrders başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PazaramaOrderDto>>(null, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<List<PazaramaOrderDto>>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Siparişler alınamadı";
                logger.LogWarning("Pazarama FetchOrders yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<List<PazaramaOrderDto>>(null, msg);
            }

            var orders = parsed.Data ?? new List<PazaramaOrderDto>();
            logger.LogInformation("Pazarama FetchOrders başarılı. Sipariş sayısı: {Count}", orders.Count);
            return new SuccessDataResult<List<PazaramaOrderDto>>(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama FetchOrders exception. StartDate: {Start}, EndDate: {End}", startDate, endDate);
            return new ErrorDataResult<List<PazaramaOrderDto>>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> UpdateOrderItemStatusAsync(long orderNumber, PazaramaOrderItemUpdate item)
    {
        try
        {
            var request = new PazaramaOrderStatusUpdateRequest(orderNumber, item);
            var response = await apiClient.PutAsync("order/updateOrderStatus", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama UpdateOrderStatus başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<object>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Durum güncellenemedi";
                logger.LogWarning("Pazarama UpdateOrderStatus yanıt başarısız: {Message}", msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("Pazarama UpdateOrderStatus başarılı. OrderNumber: {OrderNumber}, ItemId: {ItemId}",
                orderNumber, item.OrderItemId);
            return new SuccessResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama UpdateOrderStatus exception. OrderNumber: {OrderNumber}", orderNumber);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> BulkUpdateOrderStatusAsync(long orderNumber, int status)
    {
        try
        {
            var request = new PazaramaBulkOrderStatusRequest(orderNumber, status);
            var response = await apiClient.PutAsync("order/updateOrderStatusList", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama BulkUpdateOrderStatus başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<object>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Toplu durum güncellenemedi";
                logger.LogWarning("Pazarama BulkUpdateOrderStatus yanıt başarısız: {Message}", msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("Pazarama BulkUpdateOrderStatus başarılı. OrderNumber: {OrderNumber}, Status: {Status}",
                orderNumber, status);
            return new SuccessResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama BulkUpdateOrderStatus exception. OrderNumber: {OrderNumber}", orderNumber);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
