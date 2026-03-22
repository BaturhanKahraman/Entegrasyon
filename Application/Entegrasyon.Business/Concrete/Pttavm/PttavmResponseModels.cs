using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pttavm;

// --- Kategori ---

public sealed record PttavmMainCategoryResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("main_category")] List<PttavmCategoryDto>? MainCategory,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryTreeResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category_tree")] List<PttavmCategoryTreeDto>? CategoryTree,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryDetailResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category")] PttavmCategoryTreeDto? Category,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt);

public sealed record PttavmCategoryTreeDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("parent_id")] string? ParentId,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt,
    [property: JsonPropertyName("children")] List<PttavmCategoryTreeDto>? Children);

// --- Ortak ---

public sealed record PttavmError(
    [property: JsonPropertyName("error_code")] string? ErrorCode,
    [property: JsonPropertyName("error_message")] string? ErrorMessage);
