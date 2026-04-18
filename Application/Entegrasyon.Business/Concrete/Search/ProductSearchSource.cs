using Entegrasyon.Business.Abstract.Search;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Search;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Search;

public sealed class ProductSearchSource(IDbContextFactory<IntegrationDbContext> contextFactory) : ISearchSource
{
    public string SourceKey => "products";
    public string Label => "Ürünler";
    public string Icon => "ti-package";
    public string? RequiredPermission => "Permissions.Products.View";

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var q = query.Trim();

        // Barkod tam eşleşme (ProductVariant.Barcode) önceliği — tek scan
        var byBarcode = await db.ProductVariants
            .AsNoTracking()
            .Where(v => v.Barcode == q)
            .Select(v => new { v.Id, v.Barcode, v.Product.Title, ProductId = v.ProductId })
            .Take(limit)
            .ToListAsync(ct);

        var remaining = limit - byBarcode.Count;
        var byText = new List<SearchHitDto>();

        if (remaining > 0)
        {
            // tsvector ile semantic + ILIKE fallback. EF.Functions.ToTsQuery "türkçe" config'i
            // pg tarafında mevcut değilse bile Product.SearchVector tipik başlık+marka içerir.
            // Basit strateji: ILIKE %q% (Title) — pg_trgm yoksa bile çalışır, var ise hızlanır.
            var pattern = $"%{q}%";
            byText = await db.MainProducts
                .AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Title, pattern))
                .OrderByDescending(p => p.CreatedAt)
                .Take(remaining)
                .Select(p => new SearchHitDto(
                    Title: p.Title,
                    Subtitle: p.StockCode,
                    Url: $"/products/{p.Id}",
                    Score: 0.6,
                    Badge: null))
                .ToListAsync(ct);
        }

        var hits = new List<SearchHitDto>(byBarcode.Count + byText.Count);

        foreach (var v in byBarcode)
        {
            hits.Add(new SearchHitDto(
                Title: v.Title,
                Subtitle: $"Barkod: {v.Barcode}",
                Url: $"/products/{v.ProductId}",
                Score: 1.0,
                Badge: "Barkod"));
        }

        hits.AddRange(byText);

        return hits;
    }
}
