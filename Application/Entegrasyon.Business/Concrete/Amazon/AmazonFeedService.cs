using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

/// <summary>
/// Amazon Feeds API servisi.
/// Workflow: createFeedDocument → presigned URL'ye upload → createFeed → poll status.
/// </summary>
public sealed class AmazonFeedService(
    IAmazonApiClient apiClient,
    ILogger<AmazonFeedService> logger) : IAmazonFeedService
{
    public async Task<IDataResult<string>> SubmitFeedAsync(
        string feedType, string contentType, byte[] content,
        string[] marketplaceIds, CancellationToken ct = default)
    {
        try
        {
            // 1. Create feed document (get presigned URL)
            var docRequest = new AmazonCreateFeedDocumentRequest(contentType);
            var docResponse = await apiClient.PostAsync("/feeds/2021-06-30/documents", docRequest, ct);
            if (!docResponse.IsSuccessStatusCode)
            {
                var error = await docResponse.Content.ReadAsStringAsync(ct);
                return new ErrorDataResult<string>(null, $"Feed document creation failed: {docResponse.StatusCode} — {error}");
            }
            var doc = await docResponse.Content.ReadFromJsonAsync<AmazonFeedDocumentResponse>(cancellationToken: ct);
            if (doc == null)
                return new ErrorDataResult<string>(null, "Feed document response parse edilemedi.");

            // 2. Upload content to presigned URL
            var uploadResponse = await apiClient.UploadAsync(doc.Url, content, contentType, ct);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                return new ErrorDataResult<string>(null, $"Feed content upload failed: {uploadResponse.StatusCode}");
            }

            // 3. Create feed
            var feedRequest = new AmazonCreateFeedRequest(feedType, marketplaceIds.ToList(), doc.FeedDocumentId);
            var feedResponse = await apiClient.PostAsync("/feeds/2021-06-30/feeds", feedRequest, ct);
            if (!feedResponse.IsSuccessStatusCode)
            {
                var error = await feedResponse.Content.ReadAsStringAsync(ct);
                return new ErrorDataResult<string>(null, $"Feed creation failed: {feedResponse.StatusCode} — {error}");
            }
            var feed = await feedResponse.Content.ReadFromJsonAsync<AmazonCreateFeedResponse>(cancellationToken: ct);

            logger.LogInformation("Amazon feed submitted: feedId={FeedId}, type={FeedType}", feed?.FeedId, feedType);
            return new SuccessDataResult<string>(feed?.FeedId ?? "");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon feed submission failed");
            return new ErrorDataResult<string>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonFeedStatusResponse>> GetFeedStatusAsync(
        string feedId, CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.GetAsync($"/feeds/2021-06-30/feeds/{feedId}", ct);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<AmazonFeedStatusResponse>(null, $"Feed status error: {response.StatusCode}");
            var data = await response.Content.ReadFromJsonAsync<AmazonFeedStatusResponse>(cancellationToken: ct);
            return new SuccessDataResult<AmazonFeedStatusResponse>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon get feed status failed: {FeedId}", feedId);
            return new ErrorDataResult<AmazonFeedStatusResponse>(null, $"Hata: {ex.Message}");
        }
    }
}
