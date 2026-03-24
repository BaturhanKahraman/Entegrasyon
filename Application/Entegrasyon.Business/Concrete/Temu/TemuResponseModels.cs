using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Temu;

/// <summary>
/// Temu API genel response wrapper'ı.
/// Tüm API çağrıları bu yapıyı döner.
/// </summary>
public sealed class TemuApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("error_msg")]
    public string? ErrorMsg { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }
}

/// <summary>
/// Temu API hata durumunda fırlatılan exception.
/// </summary>
public sealed class TemuApiException : Exception
{
    public int ErrorCode { get; }

    public TemuApiException(int errorCode, string message)
        : base($"Temu API Error [{errorCode}]: {message}")
    {
        ErrorCode = errorCode;
    }
}

// ── Kategori modelleri ──────────────────────────────────────────────────────────

/// <summary>
/// bg.goods.cats.get response — kategori ağacı listesi.
/// </summary>
// TODO: Temu API dokümanı doğrulanınca response yapısı güncellenecek
public sealed class TemuCategoryListResponse
{
    [JsonPropertyName("cat_list")]
    public List<TemuCategoryDto> CatList { get; set; } = new();
}

/// <summary>
/// Tekil kategori DTO'su — recursive children ile ağaç yapısı.
/// </summary>
// TODO: Temu API dokümanı doğrulanınca alan adları güncellenecek
public sealed class TemuCategoryDto
{
    [JsonPropertyName("cat_id")]
    public long CatId { get; set; }

    [JsonPropertyName("cat_name")]
    public string CatName { get; set; } = string.Empty;

    [JsonPropertyName("parent_cat_id")]
    public long ParentCatId { get; set; }

    /// <summary>
    /// true ise alt kategorisi yoktur (yaprak kategori).
    /// </summary>
    [JsonPropertyName("leaf")]
    public bool Leaf { get; set; }

    /// <summary>
    /// Alt kategoriler (API recursive döndürüyorsa).
    /// </summary>
    [JsonPropertyName("children")]
    public List<TemuCategoryDto> Children { get; set; } = new();
}
