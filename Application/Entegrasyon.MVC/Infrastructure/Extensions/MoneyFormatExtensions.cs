using System.Globalization;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

public static class MoneyFormatExtensions
{
    private static readonly NumberFormatInfo TurkishLira = CreateFormat();

    private static NumberFormatInfo CreateFormat()
    {
        var nfi = (NumberFormatInfo)new CultureInfo("tr-TR").NumberFormat.Clone();
        nfi.CurrencySymbol = "₺";
        nfi.CurrencyPositivePattern = 3;
        nfi.CurrencyNegativePattern = 8;
        return nfi;
    }

    public static string ToMoney(this decimal value)
        => value.ToString("C2", TurkishLira);

    public static string ToMoney(this decimal? value)
        => (value ?? 0m).ToString("C2", TurkishLira);
}
