namespace Entegrasyon.Business.Utility.Constants;

// TODO Multi-tenant: Bu sabitler single-tenant içindir.
// Multi-tenant geçişinde ITenantContext.GetMarketPlaceId() kullanılmalıdır.
// N11SoapClient zaten ITenantContext kullanıyor — diğer servisler de geçirilmeli.
public static class MarketPlaceConstants
{
    public const int TrendyolMarketPlaceId = 1;
    public const int N11MarketPlaceId = 2;
    public const int HepsiburadaMarketPlaceId = 3;
    public const int PazaramaMarketPlaceId = 5;
    public const int AmazonMarketPlaceId = 6;
    public const int PttavmMarketPlaceId = 7;
    public const int CiceksepetiMarketPlaceId = 8;
}
