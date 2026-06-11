namespace Entegrasyon.Entity.Dtos.Brand;

/// <summary>
/// Markalar liste sayfası (GET /brands) üst KPI kartları için aggregate özet.
/// Read-path: 3 bağımsız index-backed skaler sayım (tek-sorgu/aggregate, N+1 yok),
/// hepsi soft-delete query filter'ı (!IsDeleted) altında.
/// </summary>
public sealed record BrandKpiDto(
    // Bir markaya bağlı (BrandId != null) toplam ürün sayısı — MainProducts(BrandId,IsDeleted) index'i.
    int TotalProductCount,
    // En az bir pazaryeri eşleşmesi olan distinct marka sayısı — BrandMarketPlaceMatches IX_ApplicationBrandId.
    int MatchedBrandCount,
    // Hiç ürünü olmayan marka sayısı (ProductNumber == 0) — NOT EXISTS(MainProducts.BrandId).
    int BrandsWithoutProductCount
);
