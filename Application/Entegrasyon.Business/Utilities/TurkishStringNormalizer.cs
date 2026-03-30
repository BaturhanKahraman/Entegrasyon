namespace Entegrasyon.Business.Utilities;

/// <summary>
/// Turkce karakter normalizasyonu — brand matching icin.
/// </summary>
public static class TurkishStringNormalizer
{
    private static readonly (char From, char To)[] TurkishMap =
    [
        ('ş', 's'), ('Ş', 'S'),
        ('ç', 'c'), ('Ç', 'C'),
        ('ğ', 'g'), ('Ğ', 'G'),
        ('ü', 'u'), ('Ü', 'U'),
        ('ö', 'o'), ('Ö', 'O'),
        ('ı', 'i'), ('İ', 'I'),
    ];

    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = input.ToLowerInvariant();
        foreach (var (from, to) in TurkishMap)
            result = result.Replace(from, to);
        return result;
    }
}
