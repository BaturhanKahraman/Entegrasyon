namespace Entegrasyon.Entity.Dtos.Trendyol;

/// <summary>
/// GET /integration/product/sellers/{sellerId}/products/approved?barcode={barcode}
/// Onaylı ürün filtre response'u. Barkod bu listede varsa ürün ONAYLI kabul edilir.
/// (Sadece durum senkronizasyonunda kullanılan alanlar modellendi; Trendyol'un
/// gönderdiği diğer alanlar deserializasyonda yok sayılır.)
/// </summary>
public sealed record TrendyolApprovedProductsResponse(
    int TotalElements,
    int TotalPages,
    int Page,
    int Size,
    List<TrendyolApprovedProduct>? Content);

public sealed record TrendyolApprovedProduct(
    long? ContentId,
    string? ProductMainId,
    string? Title,
    List<TrendyolApprovedVariant>? Variants);

public sealed record TrendyolApprovedVariant(
    string? Barcode,
    bool Archived,
    string? StockCode);
