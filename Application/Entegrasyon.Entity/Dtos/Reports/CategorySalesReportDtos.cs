namespace Entegrasyon.Entity.Dtos.Reports;

// ─────────────────────────────────────────────────────────────────────────────
// Kategori Bazlı Satış — gelişmiş analitik DTO'ları.
// Kanal: Mağaza = POS satışları (Sales/SaleItem); Pazaryeri + Storefront = Orders/OrderItem.
// Kategori atfı: (Sale|Order)Item → ProductVariant → Product → Category.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Bir satış kanalının (Mağaza / Storefront / pazaryeri adı) ciro ve adet toplamı.</summary>
public record ChannelSalesDto(string Channel, decimal Revenue, int Quantity);

/// <summary>Kategori sezonsal karşılaştırma — bu dönem cirosu vs geçen yıl aynı dönem.</summary>
public record CategorySeasonalDto(
    string CategoryName,
    decimal CurrentRevenue,
    decimal PreviousYearRevenue,
    double DeltaPercent);

/// <summary>Yavaş hareket eden kategori — son satıştan bu yana geçen gün (eşiği aşan = uyarı).</summary>
public record SlowMovingCategoryDto(
    string CategoryName,
    DateOnly? LastSaleDate,
    int DaysSinceLastSale,
    int HistoricalQuantity);

/// <summary>Fiyat aralığı dağılımında bir kova (satılan kalem birim fiyatına göre).</summary>
public record PriceRangeBucketDto(
    string Label,
    decimal MinPrice,
    decimal? MaxPrice,
    int LineCount,
    int Quantity,
    decimal Revenue);
