namespace Entegrasyon.Entity.Dtos.Trendyol;

/// <summary>
/// Trendyol ürün durum sorgulama response'u.
/// GET /integration/product/sellers/{sellerId}/products — barcode ile filtrelenir.
/// </summary>
public sealed record TrendyolProductStatusResponse(
    int Page,
    int Size,
    int TotalElements,
    int TotalPages,
    List<TrendyolProductContent>? Content);

public sealed record TrendyolProductContent(
    string Barcode,
    bool Approved,
    bool Archived,
    bool OnSale,
    bool Rejected,
    long? ContentId,
    long? ListingId,
    string? Title,
    string? ProductMainId,
    string? StockCode,
    decimal? SalePrice,
    decimal? ListPrice,
    int? Quantity,
    List<TrendyolRejectReason>? RejectReasonDetails);

public sealed record TrendyolRejectReason(
    string? Reason,
    string? DetailedReason);
