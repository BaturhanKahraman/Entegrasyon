namespace Entegrasyon.Entity.Sales;

/// <summary>
/// Birleşik satış kaynağı. Sale.SaleSource + Order.MarketPlaceId + Storefront'u tek enum'da toplar.
/// Marketplace değerleri (10 + MarketPlaceId) offset'i kullanır.
/// </summary>
public enum UnifiedSaleSource
{
    POS         = 1,
    Manual      = 2,
    Storefront  = 3,
    Trendyol    = 11,
    N11         = 12,
    Hepsiburada = 13,
    Amazon      = 14,
    Pazarama    = 15,
    PttAvm      = 17,
    Ciceksepeti = 18
}
