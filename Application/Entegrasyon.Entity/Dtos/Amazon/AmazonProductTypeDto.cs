using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Amazon;

/// <summary>
/// Product Type Definitions API search response.
/// GET /definitions/2020-09-01/productTypes
/// </summary>
public sealed record AmazonProductTypeSearchResponse(
    [property: JsonPropertyName("productTypes")] List<AmazonProductTypeSearchResult>? ProductTypes);

public sealed record AmazonProductTypeSearchResult(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("marketplaceIds")] List<string>? MarketplaceIds);

/// <summary>
/// Product Type Definition (JSON Schema wrapper).
/// GET /definitions/2020-09-01/productTypes/{productType}
/// </summary>
public sealed record AmazonProductTypeDefinition(
    [property: JsonPropertyName("productType")] string ProductType,
    [property: JsonPropertyName("productTypeVersion")] AmazonProductTypeVersion? ProductTypeVersion,
    [property: JsonPropertyName("schema")] AmazonJsonSchemaLink? Schema,
    [property: JsonPropertyName("requirements")] string? Requirements,
    [property: JsonPropertyName("requirementsEnforced")] string? RequirementsEnforced,
    [property: JsonPropertyName("propertyGroups")] Dictionary<string, AmazonPropertyGroup>? PropertyGroups);

public sealed record AmazonProductTypeVersion(
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("latest")] bool Latest);

public sealed record AmazonJsonSchemaLink(
    [property: JsonPropertyName("link")] AmazonLink? Link,
    [property: JsonPropertyName("checksum")] string? Checksum);

public sealed record AmazonLink(
    [property: JsonPropertyName("resource")] string? Resource,
    [property: JsonPropertyName("verb")] string? Verb);

public sealed record AmazonPropertyGroup(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("propertyNames")] List<string>? PropertyNames);
