using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Single-tenant varsayilan implementasyon.
/// Sadece gelistirme/test ortaminda kullanilir.
/// Uretimde HttpTenantContext ile Değiştirilir.
/// </summary>
public sealed class DefaultTenantContext : ITenantContext
{
    public int TenantId => 1;

    public string ConnectionString =>
        throw new InvalidOperationException("DefaultTenantContext does not support ConnectionString. Use HttpTenantContext.");

    public bool IsInitialized => true;

    public void Initialize(TenantRegistryEntry entry)
    {
        // Single-tenant modda no-op
    }

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
