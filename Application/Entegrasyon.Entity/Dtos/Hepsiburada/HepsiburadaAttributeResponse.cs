using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Hepsiburada;

/// <summary>
/// Hepsiburada kategori özellikleri yanıtı.
/// GET /api/categories/{categoryId}/attributes döner.
/// </summary>
public sealed record HepsiburadaAttributeData(
    [property: JsonPropertyName("baseAttributes")] List<HepsiburadaAttributeDto> BaseAttributes,
    [property: JsonPropertyName("attributes")] List<HepsiburadaAttributeDto> Attributes,
    [property: JsonPropertyName("variantAttributes")] List<HepsiburadaAttributeDto> VariantAttributes);

/// <summary>
/// Hepsiburada özellik tanımı.
/// </summary>
public sealed record HepsiburadaAttributeDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("mandatory")] bool Mandatory,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("multiValue")] bool MultiValue);

/// <summary>
/// Hepsiburada özellik değeri.
/// GET /api/categories/{categoryId}/attribute/{attributeId}/values döner.
/// </summary>
public sealed record HepsiburadaAttributeValueDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("value")] string Value);
