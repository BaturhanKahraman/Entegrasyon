namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Trendyol "tedarik edilemedi" (cancelorderpackageitem) sabit sebep kodları.
/// Trendyol dokümanı yalnızca kodları (500, 501, 502, 504, 505, 506) verir; etiketler generic Türkçe.
/// Bu bir "sebep yönetimi" özelliği DEĞİL — controller doğrulaması ile view seçenekleri için tek kaynak.
/// </summary>
public static class TrendyolUnsuppliedReasons
{
    public static readonly IReadOnlyList<(int Id, string Label)> All =
    [
        (500, "500 - Ürün stokta yok"),
        (501, "501 - Sebep 501"),
        (502, "502 - Sebep 502"),
        (504, "504 - Sebep 504"),
        (505, "505 - Sebep 505"),
        (506, "506 - Sebep 506"),
    ];

    public static bool IsValid(int reasonId) => All.Any(r => r.Id == reasonId);
}
