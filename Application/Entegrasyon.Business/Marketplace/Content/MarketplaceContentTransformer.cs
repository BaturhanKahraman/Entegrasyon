using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Entegrasyon.Business.Marketplace.Content;

public sealed record ContentTransformResult(string Title, string Description, IReadOnlyList<string> Warnings);

/// <summary>
/// Ürün başlık/açıklamasını hedef pazaryerinin kurallarına göre dönüştürür:
/// HTML ↔ düz metin format dönüşümü + limit uygulama (kelime-sınırında kısaltma) + aşım uyarıları.
/// Deterministik, harici bağımlılık yok (kendi HTML-strip'i — yeni paket TL onayına tabi olduğundan).
/// Kaynak: per-marketplace-product-customization-design.md (E1).
/// </summary>
public interface IMarketplaceContentTransformer
{
    /// <summary>HTML içeriği satır yapısını koruyarak düz metne çevirir (tag temizle, entity decode).</summary>
    string HtmlToPlainText(string? html);

    /// <summary>Düz metni minimal HTML'e sarar (paragraf/satır → &lt;p&gt;/&lt;br&gt;).</summary>
    string PlainTextToHtml(string? text);

    /// <summary>Verili metinde HTML tag olup olmadığını sezer.</summary>
    bool LooksLikeHtml(string? text);

    /// <summary>Başlık+açıklamayı hedef pazaryeri kurallarına göre dönüştürür; aşım uyarılarını döndürür.</summary>
    ContentTransformResult TransformForMarketplace(string? title, string? description, MarketplaceContentRules rules);
}

public sealed partial class MarketplaceContentTransformer : IMarketplaceContentTransformer
{
    [GeneratedRegex(@"<[a-zA-Z/!][^>]*>", RegexOptions.Singleline)]
    private static partial Regex AnyTagRegex();

    // Satır kıran blok-seviye kapanış/eleman tag'leri
    [GeneratedRegex(@"<\s*(br|/p|/div|/li|/h[1-6]|/tr|/ul|/ol|/table|/blockquote)\s*/?\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreakRegex();

    [GeneratedRegex(@"<\s*li[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultiNewlineRegex();

    public string HtmlToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var s = html;
        // <li> -> madde işareti, blok tag'leri -> satır sonu
        s = ListItemRegex().Replace(s, "\n• ");
        s = BlockBreakRegex().Replace(s, "\n");
        // Kalan tüm tag'leri temizle
        s = AnyTagRegex().Replace(s, string.Empty);
        // HTML entity'lerini çöz (&amp; &lt; &#231; vb.)
        s = WebUtility.HtmlDecode(s);
        // Boşluk/satır normalizasyonu — satır yapısını koru
        s = s.Replace("\r\n", "\n").Replace('\r', '\n');
        s = MultiSpaceRegex().Replace(s, " ");
        // Her satırın baş/son boşluğunu kırp
        var lines = s.Split('\n').Select(l => l.Trim());
        s = string.Join('\n', lines);
        s = MultiNewlineRegex().Replace(s, "\n\n");
        return s.Trim();
    }

    public string PlainTextToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        var paragraphs = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var p in paragraphs)
        {
            // Yalnızca anlam taşıyan HTML karakterlerini escape et — Türkçe/UTF-8 harfleri
            // numeric entity'ye (&#252;) çevirme; HTML zaten UTF-8 ve okunabilir kalmalı.
            var inner = p.Trim()
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\n", "<br>");
            sb.Append("<p>").Append(inner).Append("</p>");
        }
        return sb.ToString();
    }

    public bool LooksLikeHtml(string? text)
        => !string.IsNullOrWhiteSpace(text) && AnyTagRegex().IsMatch(text);

    public ContentTransformResult TransformForMarketplace(string? title, string? description, MarketplaceContentRules rules)
    {
        var warnings = new List<string>();

        // ── Başlık ──
        var effectiveTitle = title ?? string.Empty;
        if (!rules.TitleAllowsHtml && LooksLikeHtml(effectiveTitle))
            effectiveTitle = HtmlToPlainText(effectiveTitle);
        effectiveTitle = SmartTruncate(effectiveTitle, rules.TitleMaxLength, "Başlık", rules, warnings);

        // ── Açıklama ── format dönüşümü
        var effectiveDescription = description ?? string.Empty;
        var sourceIsHtml = LooksLikeHtml(effectiveDescription);
        if (rules.DescriptionFormat == DescriptionFormat.PlainText && sourceIsHtml)
            effectiveDescription = HtmlToPlainText(effectiveDescription);
        else if (rules.DescriptionFormat == DescriptionFormat.Html && !sourceIsHtml)
            effectiveDescription = PlainTextToHtml(effectiveDescription);

        effectiveDescription = SmartTruncate(effectiveDescription, rules.DescriptionMaxLength, "Açıklama", rules, warnings);

        return new ContentTransformResult(effectiveTitle, effectiveDescription, warnings);
    }

    private static string SmartTruncate(string value, int maxLength, string fieldName,
        MarketplaceContentRules rules, List<string> warnings)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        warnings.Add($"{fieldName} {rules.Name} limitini aşıyor ({value.Length}/{maxLength} karakter), kısaltıldı.");

        // Kelime sınırında kısalt — son boşluğa kadar geri al (limitin %20'sinden fazla kaybetme)
        var cut = value[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > maxLength * 0.8)
            cut = cut[..lastSpace];
        return cut.TrimEnd();
    }
}
