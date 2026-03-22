using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Amazon;

/// <summary>
/// Amazon Catalog Items API search response.
/// GET /catalog/2022-04-01/items
/// </summary>
public sealed record AmazonCatalogSearchResponse(
    [property: JsonPropertyName("numberOfResults")] int NumberOfResults,
    [property: JsonPropertyName("pagination")] AmazonPagination? Pagination,
    [property: JsonPropertyName("items")] List<AmazonCatalogItem>? Items);

public sealed record AmazonPagination(
    [property: JsonPropertyName("nextToken")] string? NextToken,
    [property: JsonPropertyName("previousToken")] string? PreviousToken);

/// <summary>
/// Amazon catalog item (ASIN-based).
/// </summary>
public sealed record AmazonCatalogItem(
    [property: JsonPropertyName("asin")] string Asin,
    [property: JsonPropertyName("attributes")] Dictionary<string, object>? Attributes,
    [property: JsonPropertyName("images")] List<AmazonCatalogImageSet>? Images,
    [property: JsonPropertyName("productTypes")] List<AmazonProductTypeInfo>? ProductTypes,
    [property: JsonPropertyName("summaries")] List<AmazonCatalogSummary>? Summaries,
    [property: JsonPropertyName("identifiers")] List<AmazonCatalogIdentifiers>? Identifiers);

public sealed record AmazonCatalogImageSet(
    [property: JsonPropertyName("marketplaceId")] string? MarketplaceId,
    [property: JsonPropertyName("images")] List<AmazonCatalogImage>? Images);

public sealed record AmazonCatalogImage(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("variant")] string? Variant);

public sealed record AmazonProductTypeInfo(
    [property: JsonPropertyName("marketplaceId")] string? MarketplaceId,
    [property: JsonPropertyName("productType")] string ProductType);

public sealed record AmazonCatalogSummary(
    [property: JsonPropertyName("marketplaceId")] string? MarketplaceId,
    [property: JsonPropertyName("brandName")] string? BrandName,
    [property: JsonPropertyName("itemName")] string? ItemName,
    [property: JsonPropertyName("manufacturer")] string? Manufacturer);

public sealed record AmazonCatalogIdentifiers(
    [property: JsonPropertyName("marketplaceId")] string? MarketplaceId,
    [property: JsonPropertyName("identifiers")] List<AmazonIdentifier>? Identifiers);

public sealed record AmazonIdentifier(
    [property: JsonPropertyName("identifierType")] string IdentifierType,
    [property: JsonPropertyName("identifier")] string Identifier);
