using System.Text.RegularExpressions;

namespace Entegrasyon.Business.Utilities;

public static partial class SlugHelper
{
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var slug = name.Trim().ToLowerInvariant();

        // Türkçe karakterleri ASCII eşdeğerlerine çevir
        slug = slug
            .Replace("ş", "s").Replace("Ş", "s")
            .Replace("ğ", "g").Replace("Ğ", "g")
            .Replace("ü", "u").Replace("Ü", "u")
            .Replace("ö", "o").Replace("Ö", "o")
            .Replace("ç", "c").Replace("Ç", "c")
            .Replace("ı", "i").Replace("İ", "i");

        // Alfanümerik olmayan karakterleri tire ile değiştir
        slug = NonAlphaNumeric().Replace(slug, "-");

        // Ardışık tireleri tek tireye indir ve baş/son tireleri kaldır
        slug = MultipleDashes().Replace(slug, "-").Trim('-');

        return slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleDashes();
}
