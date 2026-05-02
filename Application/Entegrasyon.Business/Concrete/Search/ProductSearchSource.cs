using Entegrasyon.Business.Abstract.Search;
using Entegrasyon.Business.Extensions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Search;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Search;

public sealed class ProductSearchSource(IDbContextFactory<IntegrationDbContext> contextFactory) : ISearchSource
{
    public string SourceKey => "products";
    public string Label => "Ürünler";
    public string Icon => "ti-package";
    // Arama tüm giriş yapmış kullanıcılara açık; gerçek ACL hit'in detay sayfasında devreye girer
    public string? RequiredPermission => null;

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var q = query.Trim();
        var hits = new List<SearchHitDto>(limit);

        // 1) Barkod tam eşleşme — en yüksek skor, sepete/detaya doğrudan gidebilsin
        var byBarcode = await db.ProductVariants
            .AsNoTracking()
            .Where(v => !v.IsDeleted && !v.Product.IsDeleted && v.Barcode == q)
            .Select(v => new { v.ProductId, ProductTitle = v.Product.Title, v.Barcode })
            .Take(limit)
            .ToListAsync(ct);

        foreach (var v in byBarcode)
        {
            hits.Add(new SearchHitDto(
                Title: v.ProductTitle,
                Subtitle: $"Barkod: {v.Barcode}",
                Url: $"/products/{v.ProductId}",
                Score: 1.0,
                Badge: "Barkod"));
        }

        var remaining = limit - hits.Count;
        if (remaining <= 0) return hits;

        // 2) Geniş/fuzzy arama — POS ile aynı strateji: tsvector OR ILIKE(Title/StockCode) OR
        //    variant barkod contains OR trigram similarity threshold. Trigram skoruyla sırala.
        var tsQuery = q.ToFullTextSearchQuery();
        var likePattern = $"%{q}%";

        var byText = await db.MainProducts
            .AsNoTracking()
            .Where(p => !p.IsDeleted && (
                p.SearchVector.Matches(tsQuery) ||
                EF.Functions.ILike(p.Title, likePattern) ||
                (p.StockCode != null && EF.Functions.ILike(p.StockCode, likePattern)) ||
                p.ProductVariants.Any(pv => !pv.IsDeleted && pv.Barcode != null && EF.Functions.ILike(pv.Barcode, likePattern)) ||
                EF.Functions.TrigramsSimilarity(p.Title, q) > 0.2
            ))
            .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Title, q))
            .Take(remaining)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.StockCode,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Score = EF.Functions.TrigramsSimilarity(p.Title, q)
            })
            .ToListAsync(ct);

        foreach (var p in byText)
        {
            // Subtitle: "StockCode · Marka" birleşik. İkisi de boşsa null.
            string? subtitle = (p.StockCode, p.BrandName) switch
            {
                (null or "", null or "") => null,
                (null or "", var b) => b,
                (var s, null or "") => s,
                (var s, var b) => $"{s} · {b}"
            };

            hits.Add(new SearchHitDto(
                Title: p.Title,
                Subtitle: subtitle,
                Url: $"/products/{p.Id}",
                // Trigram skoru 0–1. 0.4+ civarı eşleşmeyi "iyi" saymak makul, tabanda 0.3 sabit ekle.
                Score: Math.Clamp(0.3 + p.Score * 0.6, 0.3, 0.95),
                Badge: null));
        }

        return hits;
    }
}
