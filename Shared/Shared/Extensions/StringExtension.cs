namespace Shared.Extensions;

public static class StringExtension
{
    public static string NormalizeEmail(this string @this)
    {
        return string.IsNullOrEmpty(@this)?string.Empty:@this.Trim().Normalize().ToUpperInvariant();
    }

    public static void ThrowIfNullOrEmpty(this string @this)
    {
        if(string.IsNullOrEmpty(@this))
            throw new ArgumentNullException(nameof(@this));
    }
}