using System.Globalization;

namespace Entegrasyon.Business.Helpers;

/// <summary>
/// Category attribute değerlerini kanonik anahtara çevirir: trim + iç boşluk teke +
/// Türkçe-duyarlı büyük harf katlama. "Sarı"/"sarı"/"SARI"/" sarı " → "SARI".
/// Bu anahtar tekilleştirmede (dedup) ve unique index'te kullanılır.
/// </summary>
public static class AttributeValueNormalizer
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var collapsed = string.Join(' ',
            raw.Split([' ', '\t', '\n', '\r', '\f', '\v'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return collapsed.ToUpper(Tr);
    }
}
