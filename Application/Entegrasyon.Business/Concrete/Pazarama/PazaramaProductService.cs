using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama ürün publish servisi (gerçek API çağrıları).
/// Pipeline: Validation → Map → API POST → batchRequestId
/// </summary>
public sealed class PazaramaProductService(
    IPazaramaApiClient apiClient,
    IPazaramaProductMapper productMapper,
    PazaramaMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<PazaramaProductService> logger) : IPazaramaProductService
{
    private sealed record PazaramaBatchCreateData(
        [property: JsonPropertyName("batchRequestId")] string BatchRequestId);

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        // 1. Validation
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        await activityLogger.LogAsync(
            productId,
            ProductActivityType.MappingValidated,
            validationResult.Success
                ? "Pazarama mapping doğrulaması başarılı"
                : validationResult.Message!,
            validationResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
            marketplaceName: "Pazarama");

        if (!validationResult.Success)
            return new ErrorDataResult<string>(null!, validationResult.Message!);

        // 2. Mapping
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorDataResult<string>(null!, mapResult.Message!);

        // 3. API call
        try
        {
            var response = await apiClient.PostAsync("product/create", mapResult.Data);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama publish failed: {Status} {Body}", response.StatusCode, errorBody);
                await activityLogger.LogAsync(
                    productId,
                    ProductActivityType.BatchFailed,
                    $"Pazarama API hatası: {response.StatusCode}",
                    ProductActivityStatus.Error,
                    errorBody,
                    "Pazarama");
                return new ErrorDataResult<string>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content
                .ReadFromJsonAsync<PazaramaResponse<PazaramaBatchCreateData>>();

            if (parsed?.Success != true || parsed.Data?.BatchRequestId is null)
            {
                var msg = parsed?.Message ?? "batchRequestId alınamadı";
                await activityLogger.LogAsync(
                    productId,
                    ProductActivityType.BatchFailed,
                    $"Pazarama yanıt hatası: {msg}",
                    ProductActivityStatus.Error,
                    marketplaceName: "Pazarama");
                return new ErrorDataResult<string>(null!, msg);
            }

            var batchRequestId = parsed.Data.BatchRequestId;

            await activityLogger.LogAsync(
                productId,
                ProductActivityType.PublishSent,
                "Pazarama'ya ürün gönderildi",
                ProductActivityStatus.Success,
                null,
                "Pazarama",
                batchRequestId);

            return new SuccessDataResult<string>(batchRequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama publish exception for product {ProductId}", productId);
            await activityLogger.LogAsync(
                productId,
                ProductActivityType.BatchFailed,
                $"Pazarama publish hatası: {ex.Message}",
                ProductActivityStatus.Error,
                ex.ToString(),
                "Pazarama");
            return new ErrorDataResult<string>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<PazaramaBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId)
    {
        try
        {
            var response = await apiClient.GetAsync(
                $"product/getProductBatchResult?BatchRequestId={batchRequestId}");

            if (!response.IsSuccessStatusCode)
            {
                return new ErrorDataResult<PazaramaBatchStatusResponse>(
                    null!, $"Batch status API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content
                .ReadFromJsonAsync<PazaramaResponse<PazaramaBatchStatusResponse>>();

            if (parsed?.Success != true || parsed.Data is null)
            {
                return new ErrorDataResult<PazaramaBatchStatusResponse>(
                    null!, parsed?.Message ?? "Batch status bilgisi alınamadı");
            }

            return new SuccessDataResult<PazaramaBatchStatusResponse>(parsed.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama batch status check failed for {BatchRequestId}", batchRequestId);
            return new ErrorDataResult<PazaramaBatchStatusResponse>(null!, $"Hata: {ex.Message}");
        }
    }
}
