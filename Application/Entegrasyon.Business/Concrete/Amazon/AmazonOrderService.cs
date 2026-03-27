using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class AmazonOrderService(
    IAmazonApiClient apiClient,
    ILogger<AmazonOrderService> logger) : IAmazonOrderService
{
    public async Task<IDataResult<List<AmazonOrderDto>>> GetOrdersAsync(
        DateTimeOffset createdAfter, string[] marketplaceIds,
        string[]? orderStatuses = null, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&MarketplaceIds=", marketplaceIds);
            var url = $"/orders/v0/orders?MarketplaceIds={mpIds}&CreatedAfter={createdAfter:O}";
            if (orderStatuses?.Any() == true)
                url += $"&OrderStatuses={string.Join(",", orderStatuses)}";

            var response = await apiClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<List<AmazonOrderDto>>(null!,$"Orders API error: {response.StatusCode}");

            var data = await response.Content.ReadFromJsonAsync<AmazonOrderListResponse>(cancellationToken: ct);
            return new SuccessDataResult<List<AmazonOrderDto>>(data?.Payload?.Orders ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon get orders failed");
            return new ErrorDataResult<List<AmazonOrderDto>>(null!,$"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonOrderDto>> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.GetAsync($"/orders/v0/orders/{orderId}", ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<AmazonOrderDto>(null!, $"Order not found: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonOrderDto>(cancellationToken: ct);
            return new SuccessDataResult<AmazonOrderDto>(data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon get order failed: {OrderId}", orderId);
            return new ErrorDataResult<AmazonOrderDto>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<List<AmazonOrderItemDto>>> GetOrderItemsAsync(string orderId, CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.GetAsync($"/orders/v0/orders/{orderId}/orderItems", ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<List<AmazonOrderItemDto>>(null!,$"Order items error: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonOrderItemListResponse>(cancellationToken: ct);
            return new SuccessDataResult<List<AmazonOrderItemDto>>(data?.Payload?.OrderItems ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon get order items failed: {OrderId}", orderId);
            return new ErrorDataResult<List<AmazonOrderItemDto>>(null!,$"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> ConfirmShipmentAsync(string orderId, AmazonConfirmShipmentRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.PostAsync($"/orders/v0/orders/{orderId}/shipment/confirm", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Amazon confirm shipment failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorResult($"Kargo onay hatası: {response.StatusCode}");
            }
            logger.LogInformation("Amazon shipment confirmed: {OrderId}", orderId);
            return new SuccessResult("Kargo onaylandı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon confirm shipment exception: {OrderId}", orderId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
