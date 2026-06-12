using System.Globalization;

namespace Entegrasyon.Business.Helpers;

/// <summary>
/// Category attribute degerlerini kanonik anahtara cevirir: trim + ic bosluk teke +
/// Turkce-duyarli buyuk harf katlama. "Sarı"/"sarı"/"SARI"/" sarı " -> "SARI".
/// Bu anahtar tekillestirmede (dedup) ve unique index'te kullanilir.
/// </summary>
public static class AttributeValueNormalizer
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var collapsed = string.Join(' ',
            raw.Split(new[] { ' ', '\t', '\n', '\r', '\f', '\v' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return collapsed.ToUpper(Tr);
    }
}
