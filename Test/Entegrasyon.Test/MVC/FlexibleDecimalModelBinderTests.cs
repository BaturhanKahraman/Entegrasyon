using Entegrasyon.MVC.Infrastructure.ModelBinding;

namespace Entegrasyon.Test.MVC;

/// <summary>
/// T111: Para alanı decimal binder'ı hem invariant (nokta-ondalık) hem Türkçe (virgül-ondalık,
/// nokta-binlik) biçimi doğru parse etmeli. Canlı denetimde "199,90" gibi ham değer ×100/yanlış
/// parse ediliyordu (JS kapalı/HTMX/API yolu). Bu defense-in-depth onu kapatır.
/// </summary>
public class FlexibleDecimalModelBinderTests
{
    [Theory]
    // Invariant nokta-ondalık (JS normalize sonrası normal yol)
    [InlineData("199.90", 199.90)]
    [InlineData("149.9", 149.9)]
    [InlineData("1234.56", 1234.56)]
    // Türkçe virgül-ondalık (ham, JS çalışmadıysa)
    [InlineData("199,90", 199.90)]
    [InlineData("149,9", 149.9)]
    // Türkçe binlik + ondalık
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1.234.567,89", 1234567.89)]
    // US binlik + ondalık
    [InlineData("1,234.56", 1234.56)]
    // Ayraçsız tam sayı
    [InlineData("1234", 1234)]
    [InlineData("0", 0)]
    // Negatif
    [InlineData("-49,90", -49.90)]
    public void TryParseFlexible_VariousFormats_ParsesCorrectly(string raw, double expected)
    {
        var ok = FlexibleDecimalModelBinder.TryParseFlexible(raw, out var value);

        ok.Should().BeTrue();
        value.Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("199,90", 199.90)]   // KRİTİK: ×100 OLMAMALI
    [InlineData("1.234,56", 1234.56)] // KRİTİK: ×10/×100 OLMAMALI
    public void TryParseFlexible_TurkishFormat_DoesNotMultiply(string raw, double expected)
    {
        FlexibleDecimalModelBinder.TryParseFlexible(raw, out var value);
        value.Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34.56")] // geçersiz çoklu nokta (binlik değil, parse edilemez)
    [InlineData("")]
    public void TryParseFlexible_InvalidInput_ReturnsFalse(string raw)
    {
        var ok = FlexibleDecimalModelBinder.TryParseFlexible(raw, out _);
        ok.Should().BeFalse();
    }
}
