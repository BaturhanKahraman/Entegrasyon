using Entegrasyon.Business.Utilities;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Utilities;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Nike", "nike")]
    [InlineData("Adidas Türkiye", "adidas-turkiye")]
    [InlineData("  Çiçek Sepeti  ", "cicek-sepeti")]
    [InlineData("Şölen Çikolata", "solen-cikolata")]
    [InlineData("Güneş & Ay", "gunes-ay")]
    [InlineData("İstanbul Gümüş", "istanbul-gumus")]
    [InlineData("Test--Double", "test-double")]
    [InlineData("  ", "")]
    [InlineData("ABC 123", "abc-123")]
    public void GenerateSlug_VariousInputs_ReturnsExpected(string input, string expected)
    {
        SlugHelper.GenerateSlug(input).Should().Be(expected);
    }
}
