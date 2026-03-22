using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.Dtos.Hepsiburada;

/// <summary>
/// Hepsiburada API genel sayfalama wrapper'ı.
/// </summary>
public sealed record HepsiburadaApiResponse<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("data")] T? Data);

/// <summary>
/// Hepsiburada sayfalanmış kategori listesi data wrapper'ı.
/// </summary>
public sealed record HepsiburadaPaginatedData<T>(
    [property: JsonPropertyName("totalElements")] int TotalElements,
    [property: JsonPropertyName("totalPages")] int TotalPages,
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("numberOfElements")] int NumberOfElements,
    [property: JsonPropertyName("first")] bool First,
    [property: JsonPropertyName("last")] bool Last,
    [property: JsonPropertyName("content")] List<T> Content);

/// <summary>
/// Hepsiburada kategori bilgisi.
/// GET /api/categories/get-all-categories döner.
/// </summary>
public sealed record HepsiburadaCategoryDto(
    [property: JsonPropertyName("categoryId")] int CategoryId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("parentCategoryId")] int? ParentCategoryId,
    [property: JsonPropertyName("paths")] string[]? Paths,
    [property: JsonPropertyName("leaf")] bool Leaf,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("available")] bool Available);
