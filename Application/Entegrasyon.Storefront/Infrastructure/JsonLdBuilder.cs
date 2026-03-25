using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Storefront.Infrastructure;

public static class JsonLdBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string BuildStore(StorefrontSettings s)
    {
        var socialLinks = new List<string>();
        if (!string.IsNullOrWhiteSpace(s.InstagramUrl)) socialLinks.Add(s.InstagramUrl);
        if (!string.IsNullOrWhiteSpace(s.FacebookUrl)) socialLinks.Add(s.FacebookUrl);
        if (!string.IsNullOrWhiteSpace(s.TwitterUrl)) socialLinks.Add(s.TwitterUrl);
        if (!string.IsNullOrWhiteSpace(s.YouTubeUrl)) socialLinks.Add(s.YouTubeUrl);
        if (!string.IsNullOrWhiteSpace(s.TikTokUrl)) socialLinks.Add(s.TikTokUrl);

        var store = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Store",
            ["name"] = s.StoreName,
            ["telephone"] = s.ContactPhone,
            ["email"] = s.ContactEmail,
            ["address"] = new Dictionary<string, object>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = s.Address,
                ["addressLocality"] = s.District ?? s.City,
                ["addressRegion"] = s.City,
                ["addressCountry"] = "TR"
            }
        };

        if (socialLinks.Count > 0)
        {
            store["sameAs"] = socialLinks;
        }

        return JsonSerializer.Serialize(store, JsonOptions);
    }

    public static string BuildBreadcrumb(List<(string Name, string Url)> items)
    {
        var breadcrumb = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items.Select((item, index) => new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["item"] = new Dictionary<string, object>
                {
                    ["@id"] = item.Url,
                    ["name"] = item.Name
                }
            }).ToList()
        };

        return JsonSerializer.Serialize(breadcrumb, JsonOptions);
    }
}
