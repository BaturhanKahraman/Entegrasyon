namespace Entegrasyon.Entity.Categories;

/// <summary>
/// Kategori import kaynağını belirler.
/// Hem genel import yöntemlerini (CSV, API, Excel) hem de pazaryeri spesifik kaynaklarını içerir.
/// </summary>
public enum ImportSource
{
    /// <summary>
    /// Manuel oluşturulmuş kategori (UI'dan kullanıcı tarafından)
    /// </summary>
    Manual = 0,

    /// <summary>
    /// CSV dosyasından import edilmiş
    /// </summary>
    Csv = 1,

    /// <summary>
    /// API üzerinden import edilmiş (genel e-ticaret API'leri)
    /// </summary>
    Api = 2,

    /// <summary>
    /// Excel dosyasından import edilmiş
    /// </summary>
    Excel = 3,

    /// <summary>
    /// Trendyol pazaryerinden import edilmiş
    /// </summary>
    Trendyol = 100,

    /// <summary>
    /// N11 pazaryerinden import edilmiş
    /// </summary>
    N11 = 101,

    /// <summary>
    /// Hepsiburada pazaryerinden import edilmiş
    /// </summary>
    Hepsiburada = 102,

    /// <summary>
    /// Pazarama pazaryerinden import edilmiş
    /// </summary>
    Pazarama = 103,

    /// <summary>
    /// Amazon pazaryerinden import edilmiş
    /// </summary>
    Amazon = 104,

    /// <summary>
    /// PttAVM pazaryerinden import edilmiş (reserved — MarketPlaceId=7)
    /// </summary>
    PttAvm = 105,

    /// <summary>
    /// Çiçeksepeti pazaryerinden import edilmiş
    /// </summary>
    Ciceksepeti = 106
}
