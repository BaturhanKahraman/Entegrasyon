namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// Trendyol'a gönderilecek ürün verilerinin ön izlemesi (okunabilir özet).
/// Mapping ve validation'dan sonra gösterilecek payload özeti.
/// </summary>
public sealed record TrendyolSendPreviewDto(
    /// <summary>Ürünün başlığı (override varsa override, yoksa original).</summary>
    string Title,

    /// <summary>Ürünün orijinal marka adı (örn: "Nike").</summary>
    string BrandName,

    /// <summary>Trendyol'da eşleşen marka adı (örn: "NIKE").</summary>
    string TrendyolBrandName,

    /// <summary>Ürünün orijinal kategorisi (örn: "Giyim > T-Shirt").</summary>
    string CategoryName,

    /// <summary>Trendyol'da eşleşen kategori adı (örn: "Giyim > Tişört").</summary>
    string TrendyolCategoryName,

    /// <summary>Trendyol kategori ID (API'ye gönderilecek).</summary>
    int TrendyolCategoryId,

    /// <summary>Ürünün açıklaması (override varsa override, yoksa original).</summary>
    string? Description,

    /// <summary>Özellik listesi (Renk, Beden vb.).</summary>
    IReadOnlyList<TrendyolPreviewAttributeDto> Attributes,

    /// <summary>Varyant detayları (fiyat, stok, barkod).</summary>
    IReadOnlyList<TrendyolPreviewVariantDto> Variants
);

/// <summary>
/// Tek bir özellik-değer çifti (örn: Renk: Siyah).
/// </summary>
public sealed record TrendyolPreviewAttributeDto(
    /// <summary>Özellik adı (örn: "Renk").</summary>
    string Name,

    /// <summary>Özellik değeri (örn: "Siyah").</summary>
    string Value
);

/// <summary>
/// Varyant fiyatlandırma ve stok bilgileri.
/// </summary>
public sealed record TrendyolPreviewVariantDto(
    /// <summary>Varyant barkodu (örn: "ABC-S").</summary>
    string Barcode,

    /// <summary>Birleştirilmiş varyant özellikleri (örn: "Siyah / S").</summary>
    string Attributes,

    /// <summary>Liste fiyatı.</summary>
    decimal ListPrice,

    /// <summary>Satış fiyatı (override varsa override, yoksa original).</summary>
    decimal SalePrice,

    /// <summary>Stok miktarı.</summary>
    int Quantity
);
