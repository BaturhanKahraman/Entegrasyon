namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// Ürün detay sayfası (GET /products/{id}) satış/performans özeti.
/// Taban: bu MainProduct'a bağlı TÜM varyantların (ProductVariant.ProductId == productId)
/// zaman penceresi (Order.OrderDate >= now - daysPast gün, UTC) içindeki sipariş satırları.
/// Satışı olmayan üründe tüm skaler değerler 0, listeler boş (asla null).
///
/// Kategori muadili (CategoryPerformanceDto) ile aynı toplam metrikleri taşır; tek fark
/// "TopProducts" yerine "VariantBreakdown" (renk/beden gibi varyant bazlı kırılım) gelir.
/// </summary>
public sealed record ProductPerformanceDetailDto(
    Guid ProductId,
    int DaysPast,
    int TotalSoldQuantity,
    decimal TotalRevenue,
    int TotalOrderCount,
    int TotalReturnedQuantity,
    decimal ReturnRate,
    // Ağırlıklı ortalama efektif birim fiyat = TotalRevenue / brüt satılan adet (SUM(Quantity)).
    decimal AverageUnitPrice,
    IReadOnlyList<ProductVariantPerformanceDto> VariantBreakdown,
    IReadOnlyList<ProductMarketplaceBreakdownDto> MarketplaceBreakdown
);

/// <summary>
/// Varyant bazında satış kırılımı (renk/beden vb.). Ciroya göre azalan sıralı.
/// VariantName: ProductVariant.Name ?? Barcode ?? "Varyant". Barcode ayrıca taşınır ki
/// view barkodu rozet olarak gösterebilsin.
/// </summary>
public sealed record ProductVariantPerformanceDto(
    Guid VariantId,
    string VariantName,
    string? Barcode,
    int SoldQuantity,
    decimal Revenue
);

/// <summary>
/// Pazaryeri bazında kırılım. MarketPlaceId null → "Direkt/Storefront".
/// View Tabler progress bar oranını Revenue / TotalRevenue üzerinden hesaplar.
/// </summary>
public sealed record ProductMarketplaceBreakdownDto(
    int? MarketPlaceId,
    string MarketplaceName,
    int OrderCount,
    decimal Revenue
);
