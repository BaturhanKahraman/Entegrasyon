using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Amazon;

public sealed record AmazonCreateFeedDocumentRequest(
    [property: JsonPropertyName("contentType")] string ContentType);

public sealed record AmazonFeedDocumentResponse(
    [property: JsonPropertyName("feedDocumentId")] string FeedDocumentId,
    [property: JsonPropertyName("url")] string Url);

public sealed record AmazonCreateFeedRequest(
    [property: JsonPropertyName("feedType")] string FeedType,
    [property: JsonPropertyName("marketplaceIds")] List<string> MarketplaceIds,
    [property: JsonPropertyName("inputFeedDocumentId")] string InputFeedDocumentId);

public sealed record AmazonCreateFeedResponse(
    [property: JsonPropertyName("feedId")] string FeedId);

public sealed record AmazonFeedStatusResponse(
    [property: JsonPropertyName("feedId")] string FeedId,
    [property: JsonPropertyName("feedType")] string? FeedType,
    [property: JsonPropertyName("processingStatus")] string ProcessingStatus,
    [property: JsonPropertyName("resultFeedDocumentId")] string? ResultFeedDocumentId);

/// <summary>
/// Feed processing statuses: IN_QUEUE, IN_PROGRESS, DONE, CANCELLED, FATAL
/// </summary>
public static class AmazonFeedStatus
{
    public const string InQueue = "IN_QUEUE";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";
    public const string Cancelled = "CANCELLED";
    public const string Fatal = "FATAL";
}
