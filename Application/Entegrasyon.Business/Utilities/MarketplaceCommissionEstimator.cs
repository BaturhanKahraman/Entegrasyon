namespace Entegrasyon.Business.Utilities;

/// <summary>
/// Pazaryeri komisyon oranlarini tahmin eden yardimci sinif.
/// Kesin oran DB'deki MarketplaceCommissionRate tablosundan gelir;
/// bu sinif sadece hizli hesaplama ve rapor icin varsayilan oranlari saglar.
/// </summary>
public static class MarketplaceCommissionEstimator
{
    // MarketPlaceId -> varsayilan komisyon orani (%)
    private static readonly Dictionary<int, decimal> DefaultRates = new()
    {
        { 1, 13.5m },   // Trendyol  (~%12-15 arasi, ortalama %13.5)
        { 2, 10.0m },   // N11       (~%8-12 arasi, ortalama %10)
        { 3, 12.0m },   // Hepsiburada (~%10-14 arasi, ortalama %12)
        { 4, 15.0m },   // Amazon    (~%8-15 arasi, ortalama %15)
        { 5, 11.0m },   // Pazarama  (~%10-12 arasi, ortalama %11)
        { 7, 9.0m },    // PttAVM    (~%8-10 arasi, ortalama %9)
        { 8, 14.0m },   // Ciceksepeti (~%12-16 arasi, ortalama %14)
    };

    private const decimal FallbackRate = 12.0m;

    /// <summary>
    /// Verilen pazaryeri icin varsayilan komisyon oranini doner (%).
    /// Bilinmeyen marketplace icin varsayilan oran (%12) kullanilir.
    /// </summary>
    public static decimal GetDefaultRate(int marketPlaceId)
    {
        return DefaultRates.GetValueOrDefault(marketPlaceId, FallbackRate);
    }

    /// <summary>
    /// Satis fiyati uzerinden tahmini komisyon tutarini hesaplar.
    /// </summary>
    public static decimal EstimateCommission(decimal salePrice, int marketPlaceId)
    {
        if (salePrice <= 0)
            return 0m;

        var rate = GetDefaultRate(marketPlaceId);
        return Math.Round(salePrice * rate / 100m, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Satis fiyati ve maliyet uzerinden tahmini net kari hesaplar.
    /// (Satis fiyati - komisyon - maliyet)
    /// </summary>
    public static decimal EstimateNetProfit(decimal salePrice, decimal costPrice, int marketPlaceId)
    {
        if (salePrice <= 0)
            return 0m - costPrice;

        var commission = EstimateCommission(salePrice, marketPlaceId);
        return salePrice - commission - costPrice;
    }
}
