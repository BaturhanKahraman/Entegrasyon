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
    public string? RequiredPermission => "Permissions.Brands.View";

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var q = query.Trim();
        var pattern = $"%{q}%";

        var hits = await db.Brands
            .AsNoTracking()
            .Where(b => EF.Functions.ILike(b.Name, pattern))
            .OrderBy(b => b.Name.Length)
            .Take(limit)
            .Select(b => new SearchHitDto(
                Title: b.Name,
                Subtitle: null,
                Url: $"/brands/{b.Id}",
                Score: 0.65,
                Badge: null))
            .ToListAsync(ct);

        return hits;
    }
}
