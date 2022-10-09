using System.Text;
using System.Text.RegularExpressions;

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

    public static string ToBase64(this string @this)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(@this));
    }

    public static string FromBase64(this string @this)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(@this));
    }

    public static string RemoveRedundantSpaces(this string @this)
    {
        return string.IsNullOrEmpty(@this) ? string.Empty : Regex.Replace(@this,@"\s+"," ");
    }

    public static string ForFullTextSearch(this string @this)
    {
        return string.IsNullOrEmpty(@this) ? string.Empty : 
            $@"""{@this.Trim().RemoveRedundantSpaces().Replace(" ","|")}"":*" ;
        //@$"""{customerInfo.Trim().Replace(" ","|")}"":*"
    }
}