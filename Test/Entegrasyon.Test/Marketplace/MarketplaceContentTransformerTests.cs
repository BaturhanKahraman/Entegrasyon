using Entegrasyon.Business.Marketplace.Content;
using FluentAssertions;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Marketplace;

public sealed class MarketplaceContentTransformerTests
{
    private readonly MarketplaceContentTransformer _sut = new();
    private readonly MarketplaceContentRuleProvider _rules = new();

    // ── HtmlToPlainText ──

    [Fact]
    public void HtmlToPlainText_UserExample_StripsTagsKeepsText()
    {
        // Kullanıcının tam örneği (spec): Trendyol açıklamaya düz metin ister.
        var html = "<b>Kalın metin</b> normal metin.<ul><li>Madde 1</li><li>Madde 2</li></ul>";

        var result = _sut.HtmlToPlainText(html);

        result.Should().NotContain("<");
        result.Should().NotContain(">");
        result.Should().Contain("Kalın metin");
        result.Should().Contain("normal metin");
        result.Should().Contain("Madde 1");
        result.Should().Contain("Madde 2");
    }

    [Fact]
    public void HtmlToPlainText_DecodesHtmlEntities()
    {
        _sut.HtmlToPlainText("Fiyat &amp; KDV &lt;dahil&gt;")
            .Should().Be("Fiyat & KDV <dahil>");
    }

    [Fact]
    public void HtmlToPlainText_PreservesLineStructure_FromBlockTags()
    {
        var html = "<p>İlk paragraf</p><p>İkinci paragraf</p>";

        var result = _sut.HtmlToPlainText(html);

        result.Should().Contain("İlk paragraf");
        result.Should().Contain("İkinci paragraf");
        result.Should().Contain("\n");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HtmlToPlainText_NullOrWhitespace_ReturnsEmpty(string? input)
        => _sut.HtmlToPlainText(input).Should().BeEmpty();

    // ── PlainTextToHtml ──

    [Fact]
    public void PlainTextToHtml_WrapsParagraphs()
    {
        var result = _sut.PlainTextToHtml("Birinci paragraf.\n\nİkinci paragraf.");

        result.Should().Contain("<p>");
        result.Should().Contain("Birinci paragraf.");
        result.Should().Contain("İkinci paragraf.");
    }

    [Fact]
    public void PlainTextToHtml_EncodesSpecialChars()
        => _sut.PlainTextToHtml("a & b < c").Should().Contain("&amp;");

    // ── LooksLikeHtml ──

    [Theory]
    [InlineData("<b>x</b>", true)]
    [InlineData("<p>paragraf</p>", true)]
    [InlineData("düz metin & sembol", false)]
    [InlineData("5 < 10 sayısı", false)] // tag değil (harf ile başlamıyor)
    public void LooksLikeHtml_DetectsTags(string input, bool expected)
        => _sut.LooksLikeHtml(input).Should().Be(expected);

    // ── TransformForMarketplace: format ──

    [Fact]
    public void Transform_Trendyol_StripsHtmlFromDescription()
    {
        var rules = _rules.GetRules(TrendyolMarketPlaceId); // PlainText

        var result = _sut.TransformForMarketplace(
            "Ürün", "<b>Kalın</b><ul><li>Madde</li></ul>", rules);

        result.Description.Should().NotContain("<");
        result.Description.Should().Contain("Kalın");
        result.Description.Should().Contain("Madde");
    }

    [Fact]
    public void Transform_Pazarama_WrapsPlainDescriptionInHtml()
    {
        var rules = _rules.GetRules(PazaramaMarketPlaceId); // Html

        var result = _sut.TransformForMarketplace("Ürün", "Sadece düz metin", rules);

        result.Description.Should().Contain("<p>");
        result.Description.Should().Contain("Sadece düz metin");
    }

    [Fact]
    public void Transform_Pazarama_KeepsExistingHtml()
    {
        var rules = _rules.GetRules(PazaramaMarketPlaceId); // Html

        var result = _sut.TransformForMarketplace("Ürün", "<strong>zaten html</strong>", rules);

        result.Description.Should().Contain("<strong>");
    }

    // ── TransformForMarketplace: limit + uyarı ──

    [Fact]
    public void Transform_TitleOverLimit_TruncatesAndWarns()
    {
        var rules = _rules.GetRules(TrendyolMarketPlaceId); // title max 100
        var longTitle = new string('A', 150);

        var result = _sut.TransformForMarketplace(longTitle, "açıklama", rules);

        result.Title.Length.Should().BeLessThanOrEqualTo(100);
        result.Warnings.Should().ContainSingle(w => w.Contains("Başlık") && w.Contains("100"));
    }

    [Fact]
    public void Transform_DescriptionOverLimit_TruncatesAndWarns()
    {
        var rules = _rules.GetRules(AmazonMarketPlaceId); // desc max 2000
        var longDesc = new string('B', 2500);

        var result = _sut.TransformForMarketplace("Başlık", longDesc, rules);

        result.Description.Length.Should().BeLessThanOrEqualTo(2000);
        result.Warnings.Should().Contain(w => w.Contains("Açıklama") && w.Contains("2000"));
    }

    [Fact]
    public void Transform_WithinLimits_NoWarnings()
    {
        var rules = _rules.GetRules(TrendyolMarketPlaceId);

        var result = _sut.TransformForMarketplace("Kısa başlık", "Kısa açıklama", rules);

        result.Warnings.Should().BeEmpty();
        result.Title.Should().Be("Kısa başlık");
    }

    [Fact]
    public void Transform_TitleTruncation_BreaksAtWordBoundary()
    {
        var rules = _rules.GetRules(TrendyolMarketPlaceId) with { TitleMaxLength = 20 };
        // 20 karakterde kelime ortasına denk gelmesin diye boşluklu metin
        var title = "Kelime bir iki uc dort bes alti yedi";

        var result = _sut.TransformForMarketplace(title, "", rules);

        result.Title.Length.Should().BeLessThanOrEqualTo(20);
        result.Title.Should().NotEndWith(" ");
    }

    // ── Provider ──

    [Fact]
    public void RuleProvider_KnownMarketplace_ReturnsRules()
    {
        var rules = _rules.GetRules(TrendyolMarketPlaceId);

        rules.Name.Should().Be("Trendyol");
        rules.DescriptionFormat.Should().Be(DescriptionFormat.PlainText);
        rules.TitleMaxLength.Should().Be(100);
    }

    [Fact]
    public void RuleProvider_UnknownMarketplace_ReturnsSafeDefault()
    {
        var rules = _rules.GetRules(9999);

        rules.MarketPlaceId.Should().Be(9999);
        rules.TitleMaxLength.Should().BeGreaterThan(0);
        rules.DescriptionMaxLength.Should().BeGreaterThan(0);
    }
}
