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
    public string? RequiredPermission => "Permissions.Categories.View";

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var q = query.Trim();
        var pattern = $"%{q}%";

        var hits = await db.Categories
            .AsNoTracking()
            .Where(c => EF.Functions.ILike(c.Name, pattern))
            .OrderBy(c => c.Name.Length)
            .Take(limit)
            .Select(c => new SearchHitDto(
                Title: c.Name,
                Subtitle: null,
                Url: $"/categories/{c.Id}",
                Score: 0.65,
                Badge: null))
            .ToListAsync(ct);

        return hits;
    }
}
