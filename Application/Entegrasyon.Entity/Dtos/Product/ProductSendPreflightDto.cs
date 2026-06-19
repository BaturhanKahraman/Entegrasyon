namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// Preflight validation results before sending a product to Trendyol.
/// Summarizes 5 checks: category match, brand match, required attributes,
/// variant existence, and barcode coverage. AllPassed is true only if all checks succeed.
/// </summary>
public sealed record ProductSendPreflightDto(
    /// <summary>Whether the product's category has a Trendyol marketplace match.</summary>
    bool CategoryMatched,

    /// <summary>Trendyol category name if CategoryMatched is true; otherwise null.</summary>
    string? MatchedCategoryName,

    /// <summary>Whether the product's brand has a Trendyol marketplace match.</summary>
    bool BrandMatched,

    /// <summary>Trendyol brand name if BrandMatched is true; otherwise null.</summary>
    string? MatchedBrandName,

    /// <summary>Whether all required attributes have marketplace matches.</summary>
    bool RequiredAttributesMatched,

    /// <summary>List of required attribute keys that lack matches; empty if all matched.</summary>
    IReadOnlyList<string> MissingAttributes,

    /// <summary>Whether the product has at least one variant.</summary>
    bool HasVariants,

    /// <summary>Whether all variants have a barcode assigned.</summary>
    bool AllVariantsHaveBarcodes,

    /// <summary>True if all five preflight checks pass.</summary>
    bool AllPassed,

    /// <summary>Application category ID for constructing fix URLs.</summary>
    int ProductCategoryId = 0,

    /// <summary>Application category name for constructing fix URLs.</summary>
    string? ProductCategoryName = null,

    /// <summary>Application brand ID for constructing fix URLs.</summary>
    int? ProductBrandId = null,

    /// <summary>Application brand name for constructing fix URLs.</summary>
    string? ProductBrandName = null,

    /// <summary>Whether every variant has at least one image (Trendyol gönderim için zorunlu — bloklar).</summary>
    bool AllVariantsHaveImages = true,

    /// <summary>Görseli olmayan varyantların barkodları; hepsinde görsel varsa boş.</summary>
    IReadOnlyList<string>? VariantsWithoutImages = null,

    /// <summary>
    /// Bu pazaryeri için stok kaynağı (depo) eşlenmiş mi. False ise tüm varyantlar quantity:0 gider —
    /// uyarı niteliğinde (AllPassed'i bloklamaz, ortam/config sorunudur).
    /// </summary>
    bool StockSourceConfigured = true
);
