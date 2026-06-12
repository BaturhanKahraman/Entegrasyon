using Entegrasyon.Business.Helpers;
using Xunit;

namespace Entegrasyon.Test.Business;

public class AttributeValueNormalizerTests
{
    [Theory]
    [InlineData("Sarı", "SARI")]
    [InlineData("sarı", "SARI")]
    [InlineData("SARI", "SARI")]
    [InlineData("  sarı  ", "SARI")]
    [InlineData("açık   sarı", "AÇIK SARI")]   // ic bosluk teke
    [InlineData("iğne", "İĞNE")]                // i -> İ (TR)
    [InlineData("ısı", "ISI")]                   // ı -> I (TR)
    public void Normalize_ProducesCanonicalKey(string raw, string expected)
    {
        Assert.Equal(expected, AttributeValueNormalizer.Normalize(raw));
    }

    [Fact]
    public void Normalize_DifferentWords_StayDifferent()
    {
        Assert.NotEqual(
            AttributeValueNormalizer.Normalize("Açık Sarı"),
            AttributeValueNormalizer.Normalize("Sarı"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrWhitespace_ReturnsEmpty(string? raw)
    {
        Assert.Equal(string.Empty, AttributeValueNormalizer.Normalize(raw));
    }
}
