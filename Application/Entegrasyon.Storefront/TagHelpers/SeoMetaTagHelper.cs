using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.Storefront.TagHelpers;

[HtmlTargetElement("seo-meta")]
public class SeoMetaTagHelper : TagHelper
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string? CanonicalUrl { get; set; }
    public string OgType { get; set; } = "website";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        var encodedTitle = WebUtility.HtmlEncode(Title);

        output.Content.AppendHtml($"<title>{encodedTitle}</title>\n");

        if (!string.IsNullOrWhiteSpace(Description))
        {
            var encodedDesc = WebUtility.HtmlEncode(Description);
            output.Content.AppendHtml($"<meta name=\"description\" content=\"{encodedDesc}\" />\n");
        }

        if (!string.IsNullOrWhiteSpace(CanonicalUrl))
        {
            var encodedUrl = WebUtility.HtmlEncode(CanonicalUrl);
            output.Content.AppendHtml($"<link rel=\"canonical\" href=\"{encodedUrl}\" />\n");
        }

        // Open Graph tags
        output.Content.AppendHtml($"<meta property=\"og:title\" content=\"{encodedTitle}\" />\n");
        output.Content.AppendHtml($"<meta property=\"og:type\" content=\"{WebUtility.HtmlEncode(OgType)}\" />\n");
        output.Content.AppendHtml("<meta property=\"og:locale\" content=\"tr_TR\" />\n");

        if (!string.IsNullOrWhiteSpace(Description))
        {
            output.Content.AppendHtml($"<meta property=\"og:description\" content=\"{WebUtility.HtmlEncode(Description)}\" />\n");
        }

        if (!string.IsNullOrWhiteSpace(Image))
        {
            var encodedImage = WebUtility.HtmlEncode(Image);
            output.Content.AppendHtml($"<meta property=\"og:image\" content=\"{encodedImage}\" />\n");
        }

        if (!string.IsNullOrWhiteSpace(CanonicalUrl))
        {
            output.Content.AppendHtml($"<meta property=\"og:url\" content=\"{WebUtility.HtmlEncode(CanonicalUrl)}\" />\n");
        }

        // Twitter Card tags
        output.Content.AppendHtml("<meta name=\"twitter:card\" content=\"summary_large_image\" />\n");
        output.Content.AppendHtml($"<meta name=\"twitter:title\" content=\"{encodedTitle}\" />\n");

        if (!string.IsNullOrWhiteSpace(Description))
        {
            output.Content.AppendHtml($"<meta name=\"twitter:description\" content=\"{WebUtility.HtmlEncode(Description)}\" />\n");
        }

        if (!string.IsNullOrWhiteSpace(Image))
        {
            output.Content.AppendHtml($"<meta name=\"twitter:image\" content=\"{WebUtility.HtmlEncode(Image)}\" />\n");
        }
    }
}
