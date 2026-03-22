using Entegrasyon.Business.Abstract;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Single-tenant varsayılan implementasyon.
/// Multi-tenant geçişinde bu sınıf HttpContext veya auth token bazlı implementasyonla değiştirilir.
/// </summary>
public sealed class DefaultTenantContext : ITenantContext
{
    public int TenantId => 1;

    public int GetMarketPlaceId(string marketplaceName) => marketplaceName switch
    {
        "Trendyol" => TrendyolMarketPlaceId,
        "N11" => N11MarketPlaceId,
        "Hepsiburada" => HepsiburadaMarketPlaceId,
        "Pazarama" => PazaramaMarketPlaceId,
        "Amazon" => AmazonMarketPlaceId,
        _ => throw new ArgumentException($"Bilinmeyen marketplace: {marketplaceName}")
    };
}
