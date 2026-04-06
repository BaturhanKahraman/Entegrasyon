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
    string? ProductBrandName = null
);
