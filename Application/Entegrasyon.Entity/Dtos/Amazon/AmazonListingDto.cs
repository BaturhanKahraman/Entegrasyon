using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Amazon;

/// <summary>
/// Listings Items API — putListingsItem request body.
/// PUT /listings/2021-08-01/items/{sellerId}/{sku}
/// </summary>
public sealed record AmazonListingItem(
    [property: JsonPropertyName("productType")] string ProductType,
    [property: JsonPropertyName("requirements")] string? Requirements,
    [property: JsonPropertyName("attributes")] Dictionary<string, object> Attributes);

/// <summary>
/// Listings Items API — patchListingsItem request body.
/// PATCH /listings/2021-08-01/items/{sellerId}/{sku}
/// </summary>
public sealed record AmazonListingPatchRequest(
    [property: JsonPropertyName("productType")] string ProductType,
    [property: JsonPropertyName("patches")] List<AmazonListingPatch> Patches);

public sealed record AmazonListingPatch(
    [property: JsonPropertyName("op")] string Op,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("value")] List<object>? Value);

/// <summary>
/// Listings Items API — submission response.
/// Status: ACCEPTED veya INVALID.
/// </summary>
public sealed record AmazonListingSubmissionResponse(
    [property: JsonPropertyName("sku")] string? Sku,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("submissionId")] string? SubmissionId,
    [property: JsonPropertyName("issues")] List<AmazonListingIssue>? Issues);

public sealed record AmazonListingIssue(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("severity")] string? Severity,
    [property: JsonPropertyName("attributeNames")] List<string>? AttributeNames);

/// <summary>
/// Listings Items API — getListingsItem response.
/// GET /listings/2021-08-01/items/{sellerId}/{sku}
/// </summary>
public sealed record AmazonListingItemResponse(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("summaries")] List<AmazonListingSummary>? Summaries,
    [property: JsonPropertyName("attributes")] Dictionary<string, object>? Attributes,
    [property: JsonPropertyName("issues")] List<AmazonListingIssue>? Issues,
    [property: JsonPropertyName("offers")] List<AmazonListingOffer>? Offers);

public sealed record AmazonListingSummary(
    [property: JsonPropertyName("marketplaceId")] string? MarketplaceId,
    [property: JsonPropertyName("asin")] string? Asin,
    [property: JsonPropertyName("productType")] string? ProductType,
    [property: JsonPropertyName("status")] List<string>? Status,
    [property: JsonPropertyName("itemName")] string? ItemName);

public sealed record AmazonListingOffer(
    [property: JsonPropertyName("marketplaceId")] string? MarketplaceId,
    [property: JsonPropertyName("offerType")] string? OfferType,
    [property: JsonPropertyName("price")] AmazonListingPrice? Price);

public sealed record AmazonListingPrice(
    [property: JsonPropertyName("currencyCode")] string? CurrencyCode,
    [property: JsonPropertyName("amount")] decimal Amount);
