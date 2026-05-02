using Entegrasyon.Business.Abstract.Search;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Search;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Search;

public sealed class BrandSearchSource(IDbContextFactory<IntegrationDbContext> contextFactory) : ISearchSource
{
    public string SourceKey => "brands";
    public string Label => "Markalar";
    public string Icon => "ti-award";
    public string? RequiredPermission => null;

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var q = query.Trim();
        var pattern = $"%{q}%";

        // IX_Brands_Name_Trgm (AddBrandNameTrigramIndex migration) ile similarity hızlı.
        var hits = await db.Brands
            .AsNoTracking()
            .Where(b => !b.IsDeleted && (
                EF.Functions.ILike(b.Name, pattern) ||
                EF.Functions.TrigramsSimilarity(b.Name, q) > 0.2
            ))
            .OrderByDescending(b => EF.Functions.TrigramsSimilarity(b.Name, q))
            .Take(limit)
            .Select(b => new
            {
                b.Id,
                b.Name,
                Score = EF.Functions.TrigramsSimilarity(b.Name, q)
            })
            .ToListAsync(ct);

        return hits
            .Select(b => new SearchHitDto(
                Title: b.Name,
                Subtitle: null,
                Url: $"/brands/{b.Id}",
                Score: Math.Clamp(0.3 + b.Score * 0.6, 0.3, 0.95),
                Badge: null))
            .ToList();
    }
}
