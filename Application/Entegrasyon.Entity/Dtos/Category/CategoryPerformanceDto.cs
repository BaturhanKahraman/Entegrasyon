namespace Entegrasyon.Entity.Dtos.Category;

/// <summary>
/// Kategori detay sayfası (GET /categories/{id}) satış/performans özeti.
/// V1: yalnızca bu kategoriye DOĞRUDAN bağlı ürünler (MainProduct.CategoryId == categoryId),
/// alt kategori ağacı DAHİL DEĞİL. Zaman penceresi: Order.OrderDate >= now - daysPast gün (UTC).
/// Satışı olmayan kategoride tüm skaler değerler 0, listeler boş (asla null).
/// </summary>
public sealed record CategoryPerformanceDto(
    int CategoryId,
    int DaysPast,
    int TotalSoldQuantity,
    decimal TotalRevenue,
    int TotalOrderCount,
    int TotalReturnedQuantity,
    decimal ReturnRate,
    // Ağırlıklı ortalama efektif birim fiyat = TotalRevenue / brüt satılan adet (SUM(Quantity)).
    decimal AverageUnitPrice,
    IReadOnlyList<CategoryTopProductDto> TopProducts,
    IReadOnlyList<CategoryMarketplaceBreakdownDto> MarketplaceBreakdown
);

/// <summary>
/// En çok ciro yapan ürün (MainProduct bazında). View ürün adını /products/{MainProductId} linkler.
/// </summary>
public sealed record CategoryTopProductDto(
    Guid MainProductId,
    string ProductName,
    int SoldQuantity,
    decimal Revenue
);

/// <summary>
/// Pazaryeri bazında kırılım. MarketPlaceId null → "Direkt/Storefront".
/// View Tabler progress bar oranını Revenue / TotalRevenue üzerinden hesaplar.
/// </summary>
public sealed record CategoryMarketplaceBreakdownDto(
    int? MarketPlaceId,
    string MarketplaceName,
    int OrderCount,
    decimal Revenue
);
