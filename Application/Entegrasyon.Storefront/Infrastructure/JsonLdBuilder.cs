using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Entity.Dtos.Storefront;
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

    public static string BuildProduct(StorefrontProductDetailDto p, string baseUrl)
    {
        var images = p.Variants.SelectMany(v => v.ImageUrls).Distinct().ToList();
        var totalStock = p.Variants.Sum(v => v.Stock);
        var availability = totalStock > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock";

        var product = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = p.Title,
            ["url"] = $"{baseUrl}/urun/{p.SeoSlug}",
            ["offers"] = new Dictionary<string, object>
            {
                ["@type"] = "AggregateOffer",
                ["lowPrice"] = p.MinPrice,
                ["highPrice"] = p.MaxPrice,
                ["priceCurrency"] = "TRY",
                ["availability"] = availability,
                ["itemCondition"] = "https://schema.org/NewCondition"
            }
        };

        if (p.Description is not null) product["description"] = p.Description;
        if (p.StockCode is not null) product["sku"] = p.StockCode;
        if (images.Count > 0) product["image"] = images;
        if (p.BrandName is not null)
            product["brand"] = new Dictionary<string, object> { ["@type"] = "Brand", ["name"] = p.BrandName };

        return JsonSerializer.Serialize(product, JsonOptions);
    }

    public static string BuildCollectionPage(string name, string? description, string url)
    {
        var page = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "CollectionPage",
            ["name"] = name,
            ["url"] = url
        };
        if (description is not null) page["description"] = description;
        return JsonSerializer.Serialize(page, JsonOptions);
    }
}
