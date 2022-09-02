namespace Shared.Extensions;

public static class StringExtension
{
    public static string NormalizeEmail(this string @this)
    {
        return @this.Trim().Normalize().ToUpperInvariant();
    }

    public static void ThrowIfNullOrEmpty(this string @this)
    {
        if(string.IsNullOrEmpty(@this))
            throw new ArgumentNullException(nameof(@this));
    }
}