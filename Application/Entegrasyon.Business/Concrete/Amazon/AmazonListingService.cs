using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class AmazonListingService(
    IAmazonApiClient apiClient,
    ILogger<AmazonListingService> logger) : IAmazonListingService
{
    public async Task<IDataResult<AmazonListingSubmissionResponse>> PutListingItemAsync(
        string sellerId, string sku, AmazonListingItem item, string[] marketplaceIds, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&marketplaceIds=", marketplaceIds);
            var url = $"/listings/2021-08-01/items/{sellerId}/{Uri.EscapeDataString(sku)}?marketplaceIds={mpIds}";
            var response = await apiClient.PutAsync(url, item, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Amazon putListingItem failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorDataResult<AmazonListingSubmissionResponse>(null, $"Listing oluşturma hatası: {response.StatusCode}");
            }
            var result = await response.Content.ReadFromJsonAsync<AmazonListingSubmissionResponse>(cancellationToken: ct);
            return new SuccessDataResult<AmazonListingSubmissionResponse>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon putListingItem exception: {Sku}", sku);
            return new ErrorDataResult<AmazonListingSubmissionResponse>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonListingSubmissionResponse>> PatchListingItemAsync(
        string sellerId, string sku, AmazonListingPatchRequest patches, string[] marketplaceIds, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&marketplaceIds=", marketplaceIds);
            var url = $"/listings/2021-08-01/items/{sellerId}/{Uri.EscapeDataString(sku)}?marketplaceIds={mpIds}";
            var response = await apiClient.PatchAsync(url, patches, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new ErrorDataResult<AmazonListingSubmissionResponse>(null, $"Listing güncelleme hatası: {response.StatusCode}");
            }
            var result = await response.Content.ReadFromJsonAsync<AmazonListingSubmissionResponse>(cancellationToken: ct);
            return new SuccessDataResult<AmazonListingSubmissionResponse>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon patchListingItem exception: {Sku}", sku);
            return new ErrorDataResult<AmazonListingSubmissionResponse>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonListingItemResponse>> GetListingItemAsync(
        string sellerId, string sku, string[] marketplaceIds, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&marketplaceIds=", marketplaceIds);
            var url = $"/listings/2021-08-01/items/{sellerId}/{Uri.EscapeDataString(sku)}?marketplaceIds={mpIds}&includedData=summaries,attributes,issues,offers";
            var response = await apiClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<AmazonListingItemResponse>(null, $"Listing bulunamadı: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonListingItemResponse>(cancellationToken: ct);
            return new SuccessDataResult<AmazonListingItemResponse>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon getListingItem exception: {Sku}", sku);
            return new ErrorDataResult<AmazonListingItemResponse>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> DeleteListingItemAsync(
        string sellerId, string sku, string[] marketplaceIds, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&marketplaceIds=", marketplaceIds);
            var url = $"/listings/2021-08-01/items/{sellerId}/{Uri.EscapeDataString(sku)}?marketplaceIds={mpIds}";
            var response = await apiClient.DeleteAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorResult($"Listing silme hatası: {response.StatusCode}");
            logger.LogInformation("Amazon listing deleted: {Sku}", sku);
            return new SuccessResult("Listing silindi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon deleteListingItem exception: {Sku}", sku);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
