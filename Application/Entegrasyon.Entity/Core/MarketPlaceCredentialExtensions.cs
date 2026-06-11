namespace Entegrasyon.Entity;

/// <summary>
/// Pazaryeri API anahtarlarının tam yapılandırılıp yapılandırılmadığını tespit eden saf-fonksiyon.
/// Tek kaynak: hem Sync Overview kart durumu (MVC) hem sync guard (Business) bunu kullanır.
/// Kural (spec 2026-06-11-marketplace-sync-credential-disabled):
///  - Amazon/Pazarama OAuth2 (Id=4/5): TokenUrl + RefreshToken
///  - Trendyol (Id=1): ApiKey + ApiSecret + SellerId
///  - Diğer: ApiKey + ApiSecret
/// </summary>
public static class MarketPlaceCredentialExtensions
{
    public static bool IsCredentialComplete(this MarketPlace mp) =>
        mp.Id is 4 or 5
            ? !string.IsNullOrEmpty(mp.TokenUrl) && !string.IsNullOrEmpty(mp.RefreshToken)
            : !string.IsNullOrEmpty(mp.ApiKey) && !string.IsNullOrEmpty(mp.ApiSecret)
              && (mp.Id != 1 || !string.IsNullOrEmpty(mp.SellerId));
}
