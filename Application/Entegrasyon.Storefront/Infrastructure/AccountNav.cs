namespace Entegrasyon.Storefront.Infrastructure;

/// <summary>
/// Hesap ("sol menü") aktif öğe çözümü. Aktif öğe istek path'inden türetilir;
/// böylece her route kendi menü öğesini aktif gösterir ve eksik/yanlış model key
/// gelse bile menü ilk öğeye ("dashboard") düşüp asılı kalmaz.
/// </summary>
public static class AccountNav
{
    // Href → key eşlemesi. _AccountSidebar menü öğeleriyle birebir aynı.
    // Sıra önemsiz: en uzun (en spesifik) prefix kazanır, böylece
    // "/hesabim/guvenlik/2fa" => twofa, "/hesabim/guvenlik" => security ayrışır.
    private static readonly (string Href, string Key)[] Routes =
    [
        ("/hesabim/siparislerim",    "orders"),
        ("/hesabim/Sipariş",         "orders"),
        ("/hesabim/iadelerim",       "returns"),
        ("/hesabim/iade-talebi",     "returns"),
        ("/hesabim/tekrar-satin-al", "buyagain"),
        ("/favorilerim",             "wishlist"),
        ("/hesabim/cuzdanim",        "wallet"),
        ("/hesabim/puan-programi",   "loyalty"),
        ("/hesabim/arkadasini-getir","referral"),
        ("/hesabim/profil",          "profile"),
        ("/hesabim/e-posta-degistir","profile"),
        ("/hesabim/adresler",        "addresses"),
        ("/hesabim/guvenlik/2fa",    "twofa"),
        ("/hesabim/guvenlik",        "security"),
        ("/hesabim/sifre",           "password"),
    ];

    private const string Fallback = "dashboard";

    /// <summary>
    /// Aktif menü key'ini döndürür. <paramref name="explicitKey"/> verilmişse o kullanılır
    /// (geriye dönük uyumluluk / özel durumlar). Aksi halde path'ten en spesifik eşleşme,
    /// hiçbiri yoksa "dashboard" döner.
    /// </summary>
    public static string ResolveActiveKey(string? path, string? explicitKey)
    {
        if (!string.IsNullOrWhiteSpace(explicitKey))
            return explicitKey;

        if (string.IsNullOrWhiteSpace(path))
            return Fallback;

        var p = path.TrimEnd('/');
        if (p.Length == 0)
            return Fallback;

        var best = Fallback;
        var bestLen = 0;
        foreach (var (href, key) in Routes)
        {
            // Tam eşleşme ya da "/href/..." alt yolu — "/hesabim/profilim" gibi
            // yanlış pozitifleri önlemek için segment sınırı (sonraki karakter '/').
            var isMatch = p.Equals(href, StringComparison.OrdinalIgnoreCase)
                || (p.Length > href.Length
                    && p[href.Length] == '/'
                    && p.StartsWith(href, StringComparison.OrdinalIgnoreCase));

            if (isMatch && href.Length > bestLen)
            {
                best = key;
                bestLen = href.Length;
            }
        }

        return best;
    }
}
