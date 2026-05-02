using Entegrasyon.Business.Abstract.Search;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Search;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Search;

public sealed class CategorySearchSource(IDbContextFactory<IntegrationDbContext> contextFactory) : ISearchSource
{
    public string SourceKey => "categories";
    public string Label => "Kategoriler";
    public string Icon => "ti-category";
    public string? RequiredPermission => null;

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var q = query.Trim();
        var pattern = $"%{q}%";

        // IX_Categories_Name_Trgm (pg_trgm GIN) mevcut — similarity ve ILIKE hızlı.
        // ILIKE kısa queryler için; trigram similarity > 0.2 typo tolerant fallback.
        var hits = await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && (
                EF.Functions.ILike(c.Name, pattern) ||
                EF.Functions.TrigramsSimilarity(c.Name, q) > 0.2
            ))
            .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.Name, q))
            .Take(limit)
            .Select(c => new
            {
                c.Id,
                c.Name,
                Score = EF.Functions.TrigramsSimilarity(c.Name, q)
            })
            .ToListAsync(ct);

        return hits
            .Select(c => new SearchHitDto(
                Title: c.Name,
                Subtitle: null,
                Url: $"/categories/{c.Id}",
                Score: Math.Clamp(0.3 + c.Score * 0.6, 0.3, 0.95),
                Badge: null))
            .ToList();
    }
}
