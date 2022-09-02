namespace Shared.Extensions;

public static class StringExtension
{
    public static string NormalizeEmail(this string @this)
    {
        return @this.Trim().Normalize().ToUpperInvariant();
    }
}