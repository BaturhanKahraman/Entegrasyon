using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class AmazonProductTypeService(
    IAmazonApiClient apiClient,
    ILogger<AmazonProductTypeService> logger) : IAmazonProductTypeService
{
    public async Task<IDataResult<List<AmazonProductTypeSearchResult>>> SearchProductTypesAsync(
        string keywords, string marketplaceId, CancellationToken ct = default)
    {
        try
        {
            var url = $"/definitions/2020-09-01/productTypes?keywords={Uri.EscapeDataString(keywords)}&marketplaceIds={marketplaceId}";
            var response = await apiClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<List<AmazonProductTypeSearchResult>>(null!,$"Product type search failed: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonProductTypeSearchResponse>(cancellationToken: ct);
            return new SuccessDataResult<List<AmazonProductTypeSearchResult>>(data?.ProductTypes ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon product type search failed");
            return new ErrorDataResult<List<AmazonProductTypeSearchResult>>(null!,$"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonProductTypeDefinition>> GetProductTypeDefinitionAsync(
        string productType, string marketplaceId, string? requirements = "LISTING", CancellationToken ct = default)
    {
        try
        {
            var url = $"/definitions/2020-09-01/productTypes/{productType}?marketplaceIds={marketplaceId}&requirements={requirements}";
            var response = await apiClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<AmazonProductTypeDefinition>(null!, $"Product type definition not found: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonProductTypeDefinition>(cancellationToken: ct);
            return new SuccessDataResult<AmazonProductTypeDefinition>(data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon get product type definition failed: {ProductType}", productType);
            return new ErrorDataResult<AmazonProductTypeDefinition>(null!, $"Hata: {ex.Message}");
        }
    }
}
