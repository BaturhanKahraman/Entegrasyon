using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Entegrasyon.Business.Utilities;

/// <summary>
/// Şube ofisi isimlerini filtered unique index için normalize eder.
///
/// Neden manuel Türkçe map?
/// ToUpperInvariant() "İ" (U+0130) karakterini olduğu gibi bırakır, "ı" (U+0131) ise "I" olur —
/// sonuç: "İstanbul".ToUpperInvariant() = "İSTANBUL", "istanbul".ToUpperInvariant() = "ISTANBUL" (farklı string).
/// FormKD decompose ederse "İ" → "I + combining dot above" olur, combining marks ToUpperInvariant ile temizlenmez.
///
/// Tek güvenli yol: önce Türkçe karakterleri ASCII eşdeğerlerine manuel map et,
/// sonra latin diakritikleri (combining marks) strip et, en son ToUpperInvariant uygula.
/// </summary>
public static class BranchNameNormalizer
{
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // 1) Trim + çoklu boşlukları tek boşluğa indir
        var trimmed = Regex.Replace(name.Trim(), @"\s+", " ");

        // 2) Türkçe karakterleri ASCII eşdeğerlerine çevir
        var sbRepl = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            sbRepl.Append(ch switch
            {
                'İ' => 'I', 'ı' => 'i',
                'Ş' => 'S', 'ş' => 's',
                'Ğ' => 'G', 'ğ' => 'g',
                'Ü' => 'U', 'ü' => 'u',
                'Ö' => 'O', 'ö' => 'o',
                'Ç' => 'C', 'ç' => 'c',
                _ => ch
            });
        }

        // 3) Kalan diakritikleri strip et (é, à, ñ gibi latin combining marks)
        var decomposed = sbRepl.ToString().Normalize(NormalizationForm.FormD);
        var sbStrip = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sbStrip.Append(ch);
        }

        // 4) Recompose + invariant upper (artık sadece ASCII olduğu için invariant güvenli)
        return sbStrip.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
