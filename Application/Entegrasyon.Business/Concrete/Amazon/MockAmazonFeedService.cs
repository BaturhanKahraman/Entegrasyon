using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class MockAmazonFeedService(ILogger<MockAmazonFeedService> logger) : IAmazonFeedService
{
    public Task<IDataResult<string>> SubmitFeedAsync(
        string feedType, string contentType, byte[] content,
        string[] marketplaceIds, CancellationToken ct = default)
    {
        var mockFeedId = $"mock-feed-{Guid.NewGuid():N}"[..24];
        logger.LogInformation("[MOCK] Amazon feed submitted: feedId={FeedId}, type={FeedType}, size={Size}",
            mockFeedId, feedType, content.Length);
        return Task.FromResult<IDataResult<string>>(new SuccessDataResult<string>(mockFeedId));
    }

    public Task<IDataResult<AmazonFeedStatusResponse>> GetFeedStatusAsync(
        string feedId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon feed status: {FeedId}", feedId);
        var response = new AmazonFeedStatusResponse(feedId, "JSON_LISTINGS_FEED", AmazonFeedStatus.Done, null);
        return Task.FromResult<IDataResult<AmazonFeedStatusResponse>>(new SuccessDataResult<AmazonFeedStatusResponse>(response));
    }
}
