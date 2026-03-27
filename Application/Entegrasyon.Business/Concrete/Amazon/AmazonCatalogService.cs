using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class AmazonCatalogService(
    IAmazonApiClient apiClient,
    ILogger<AmazonCatalogService> logger) : IAmazonCatalogService
{
    public async Task<IDataResult<AmazonCatalogSearchResponse>> SearchCatalogItemsAsync(
        string keywords, string[] marketplaceIds, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&marketplaceIds=", marketplaceIds);
            var url = $"/catalog/2022-04-01/items?keywords={Uri.EscapeDataString(keywords)}&marketplaceIds={mpIds}&includedData=summaries,identifiers,images,productTypes";
            var response = await apiClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<AmazonCatalogSearchResponse>(null!, $"Catalog search failed: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonCatalogSearchResponse>(cancellationToken: ct);
            return new SuccessDataResult<AmazonCatalogSearchResponse>(data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon catalog search failed");
            return new ErrorDataResult<AmazonCatalogSearchResponse>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonCatalogItem>> GetCatalogItemAsync(
        string asin, string[] marketplaceIds, string[]? includedData = null, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&marketplaceIds=", marketplaceIds);
            var included = includedData != null ? string.Join(",", includedData) : "summaries,identifiers,images,productTypes,attributes";
            var url = $"/catalog/2022-04-01/items/{asin}?marketplaceIds={mpIds}&includedData={included}";
            var response = await apiClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<AmazonCatalogItem>(null!, $"Catalog item not found: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonCatalogItem>(cancellationToken: ct);
            return new SuccessDataResult<AmazonCatalogItem>(data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon get catalog item failed: {Asin}", asin);
            return new ErrorDataResult<AmazonCatalogItem>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonCatalogSearchResponse>> SearchByIdentifierAsync(
        string identifier, string identifierType, string[] marketplaceIds, CancellationToken ct = default)
    {
        try
        {
            var mpIds = string.Join("&marketplaceIds=", marketplaceIds);
            var url = $"/catalog/2022-04-01/items?identifiers={Uri.EscapeDataString(identifier)}&identifiersType={identifierType}&marketplaceIds={mpIds}&includedData=summaries,identifiers,images,productTypes";
            var response = await apiClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<AmazonCatalogSearchResponse>(null!, $"Identifier search failed: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonCatalogSearchResponse>(cancellationToken: ct);
            return new SuccessDataResult<AmazonCatalogSearchResponse>(data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon identifier search failed: {Identifier}", identifier);
            return new ErrorDataResult<AmazonCatalogSearchResponse>(null!, $"Hata: {ex.Message}");
        }
    }
}
