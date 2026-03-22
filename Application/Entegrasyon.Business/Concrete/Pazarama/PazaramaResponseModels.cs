using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed record PazaramaResponse<T>(
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("messageCode")] string? MessageCode,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("userMessage")] string? UserMessage,
    [property: JsonPropertyName("fromCache")] bool FromCache);

public sealed record PazaramaCategoryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("parentId")] Guid? ParentId,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("parentCategories")] List<string>? ParentCategories,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("displayOrder")] int DisplayOrder,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("leaf")] bool Leaf);

public sealed record PazaramaCategoryWithAttributesDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("attributes")] List<PazaramaCategoryAttributeDto> Attributes);

public sealed record PazaramaCategoryAttributeDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("isVariantable")] bool IsVariantable,
    [property: JsonPropertyName("isRequired")] bool IsRequired,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("attributeValues")] List<PazaramaCategoryAttributeValueDto> AttributeValues);

public sealed record PazaramaCategoryAttributeValueDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("value")] string Value);

public sealed record PazaramaBrandDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("logoUrl")] string? LogoUrl,
    [property: JsonPropertyName("website")] string? Website,
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("seoName")] string? SeoName);
