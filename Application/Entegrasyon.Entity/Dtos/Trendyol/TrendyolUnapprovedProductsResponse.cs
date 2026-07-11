namespace Entegrasyon.Entity.Dtos.Trendyol;

/// <summary>
/// GET /integration/product/sellers/{sellerId}/products/unapproved?barcode={barcode}
/// Onaysız/red ürün filtre response'u. rejectReasonDetails doluysa ürün REDDEDİLMİŞ,
/// boşsa henüz inceleniyor (pendingApproval) kabul edilir.
/// </summary>
public sealed record TrendyolUnapprovedProductsResponse(
    int TotalElements,
    int TotalPages,
    int Page,
    int Size,
    List<TrendyolUnapprovedProduct>? Content);

public sealed record TrendyolUnapprovedProduct(
    string? Barcode,
    string? ProductMainId,
    string? StockCode,
    string? Title,
    List<TrendyolRejectReason>? RejectReasonDetails);

public sealed record TrendyolRejectReason(
    string? Reason,
    string? DetailedReason);
