using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Marketplace.Content;

/// <summary>Açıklama alanının hedef pazaryerinde kabul ettiği format.</summary>
public enum DescriptionFormat
{
    PlainText,
    Html
}

/// <summary>Zorunlu attribute eşleşmediğinde gönderim davranışı.</summary>
public enum RequiredAttributeEnforcement
{
    Block,
    Warn,
    Skip
}

/// <summary>
/// Bir pazaryerinin içerik (başlık/açıklama/görsel/attribute) kuralları — tek merkezî kaynak.
/// Mapper'lardaki dağınık magic-number'ların (başlık [..100], açıklama [..30000] vb.) yerini alır.
///
/// Kaynak: docs/superpowers/specs/2026-06-10-per-marketplace-product-customization-design.md (E2).
/// NOT (WireMock strict-rule): limitler/format en doğru hali için ilgili pazaryerinin RESMİ
/// dokümanından teyit edilmeli. Aşağıdaki değerler mevcut mapper magic-number'ları + spec'ten
/// türetilmiş başlangıç setidir; doğrulama task'ı T110 kapsamındadır.
/// </summary>
public sealed record MarketplaceContentRules
{
    public required int MarketPlaceId { get; init; }
    public required string Name { get; init; }
    public DescriptionFormat DescriptionFormat { get; init; } = DescriptionFormat.PlainText;
    public bool TitleAllowsHtml { get; init; }
    public int TitleMaxLength { get; init; } = 100;
    public int DescriptionMaxLength { get; init; } = 30000;
    public int MaxImages { get; init; } = 8;
    public bool AllowsCustomAttributeValue { get; init; } = true;
    public RequiredAttributeEnforcement RequiredAttributeEnforcement { get; init; } = RequiredAttributeEnforcement.Warn;
}

/// <summary>
/// Pazaryeri içerik kurallarını sağlar. Kurallar şu an global/sabit (kod) — multi-tenant geçişinde
/// tenant-bazlı override DB'den okunabilir (bu arayüz o esnekliği korur).
/// </summary>
public interface IMarketplaceContentRuleProvider
{
    MarketplaceContentRules GetRules(int marketPlaceId);
}

public sealed class MarketplaceContentRuleProvider : IMarketplaceContentRuleProvider
{
    private static readonly IReadOnlyDictionary<int, MarketplaceContentRules> Rules =
        new Dictionary<int, MarketplaceContentRules>
        {
            [TrendyolMarketPlaceId] = new()
            {
                MarketPlaceId = TrendyolMarketPlaceId, Name = "Trendyol",
                DescriptionFormat = DescriptionFormat.PlainText, TitleAllowsHtml = false,
                TitleMaxLength = 100, DescriptionMaxLength = 30000, MaxImages = 8,
                AllowsCustomAttributeValue = true
            },
            [N11MarketPlaceId] = new()
            {
                MarketPlaceId = N11MarketPlaceId, Name = "N11",
                DescriptionFormat = DescriptionFormat.Html, TitleAllowsHtml = false,
                TitleMaxLength = 100, DescriptionMaxLength = 20000, MaxImages = 8
            },
            [HepsiburadaMarketPlaceId] = new()
            {
                MarketPlaceId = HepsiburadaMarketPlaceId, Name = "Hepsiburada",
                DescriptionFormat = DescriptionFormat.PlainText, TitleAllowsHtml = false,
                TitleMaxLength = 500, DescriptionMaxLength = 30000, MaxImages = 5
            },
            [PazaramaMarketPlaceId] = new()
            {
                MarketPlaceId = PazaramaMarketPlaceId, Name = "Pazarama",
                DescriptionFormat = DescriptionFormat.Html, TitleAllowsHtml = false,
                TitleMaxLength = 200, DescriptionMaxLength = 50000, MaxImages = 8,
                AllowsCustomAttributeValue = false
            },
            [AmazonMarketPlaceId] = new()
            {
                MarketPlaceId = AmazonMarketPlaceId, Name = "Amazon",
                DescriptionFormat = DescriptionFormat.PlainText, TitleAllowsHtml = false,
                TitleMaxLength = 200, DescriptionMaxLength = 2000, MaxImages = 9
            },
            [PttavmMarketPlaceId] = new()
            {
                MarketPlaceId = PttavmMarketPlaceId, Name = "Pttavm",
                DescriptionFormat = DescriptionFormat.Html, TitleAllowsHtml = false,
                TitleMaxLength = 200, DescriptionMaxLength = 30000, MaxImages = 8
            },
            [CiceksepetiMarketPlaceId] = new()
            {
                MarketPlaceId = CiceksepetiMarketPlaceId, Name = "Çiçeksepeti",
                DescriptionFormat = DescriptionFormat.Html, TitleAllowsHtml = false,
                TitleMaxLength = 255, DescriptionMaxLength = 30000, MaxImages = 8
            },
            [TemuMarketPlaceId] = new()
            {
                MarketPlaceId = TemuMarketPlaceId, Name = "Temu",
                DescriptionFormat = DescriptionFormat.PlainText, TitleAllowsHtml = false,
                TitleMaxLength = 200, DescriptionMaxLength = 30000, MaxImages = 8
            },
        };

    public MarketplaceContentRules GetRules(int marketPlaceId)
        => Rules.TryGetValue(marketPlaceId, out var r)
            ? r
            : new MarketplaceContentRules { MarketPlaceId = marketPlaceId, Name = $"Marketplace-{marketPlaceId}" };
}
