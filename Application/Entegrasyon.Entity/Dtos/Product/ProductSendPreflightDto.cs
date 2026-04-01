namespace Entegrasyon.Entity.Dtos.Product;

public record ProductSendPreflightDto(
    bool CategoryMatched,
    string? MatchedCategoryName,
    bool BrandMatched,
    string? MatchedBrandName,
    bool RequiredAttributesMatched,
    List<string> MissingAttributes,
    bool HasVariants,
    bool AllVariantsHaveBarcodes,
    bool AllPassed
);
