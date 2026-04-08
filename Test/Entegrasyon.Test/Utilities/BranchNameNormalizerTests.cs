using Entegrasyon.Business.Utilities;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Utilities;

/// <summary>
/// Faz 3: BranchNameNormalizer test matrisi.
/// Türkçe locale edge case'leri (özellikle ı/İ), diakritikler ve whitespace collapsing.
/// </summary>
public class BranchNameNormalizerTests
{
    [Theory]
    [InlineData("istanbul")]
    [InlineData("İstanbul")]
    [InlineData("ISTANBUL")]
    [InlineData(" istanbul ")]
    [InlineData("İSTANBUL")]
    public void Normalize_turkish_dotted_i_collides_with_ascii_i(string input)
    {
        BranchNameNormalizer.Normalize(input).Should().Be("ISTANBUL");
    }

    [Theory]
    [InlineData("Şube")]
    [InlineData("şube")]
    [InlineData("SUBE")]
    [InlineData("sube")]
    public void Normalize_turkish_s_with_cedilla_collides_with_ascii_s(string input)
    {
        BranchNameNormalizer.Normalize(input).Should().Be("SUBE");
    }

    [Theory]
    [InlineData("Çiğdem")]
    [InlineData("çiğdem")]
    [InlineData("CIGDEM")]
    [InlineData("cigdem")]
    public void Normalize_turkish_g_with_breve_and_c_cedilla(string input)
    {
        BranchNameNormalizer.Normalize(input).Should().Be("CIGDEM");
    }

    [Theory]
    [InlineData("Café", "CAFE")]
    [InlineData("résumé", "RESUME")]
    [InlineData("piñata", "PINATA")]
    public void Normalize_strips_latin_diacritics(string input, string expected)
    {
        BranchNameNormalizer.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_collapses_inner_whitespace()
    {
        BranchNameNormalizer.Normalize("İstanbul  Deposu").Should().Be("ISTANBUL DEPOSU");
        BranchNameNormalizer.Normalize("istanbul deposu").Should().Be("ISTANBUL DEPOSU");
        BranchNameNormalizer.Normalize("  istanbul\tdeposu  ").Should().Be("ISTANBUL DEPOSU");
    }

    [Fact]
    public void Normalize_different_names_do_not_collide()
    {
        BranchNameNormalizer.Normalize("Istanbul").Should().NotBe(BranchNameNormalizer.Normalize("Ankara"));
        BranchNameNormalizer.Normalize("Şube A").Should().NotBe(BranchNameNormalizer.Normalize("Şube B"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    [InlineData(null)]
    public void Normalize_empty_or_whitespace_returns_empty(string? input)
    {
        BranchNameNormalizer.Normalize(input!).Should().Be(string.Empty);
    }

    [Fact]
    public void Normalize_full_turkish_alphabet_mapping()
    {
        BranchNameNormalizer.Normalize("İşığüöç").Should().Be("ISIGUOC");
        BranchNameNormalizer.Normalize("ıiİI").Should().Be("IIII");
    }

    [Fact]
    public void Normalize_turkish_case_insensitivity_matrix()
    {
        var variants = new[] { "İstanbul", "istanbul", "ISTANBUL", "İSTANBUL", " iStAnBuL " };
        var normalized = variants.Select(BranchNameNormalizer.Normalize).Distinct().ToList();
        normalized.Should().HaveCount(1, "tüm varyantlar aynı normalized değere eşit olmalı");
        normalized[0].Should().Be("ISTANBUL");
    }
}
