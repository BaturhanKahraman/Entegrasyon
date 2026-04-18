using Entegrasyon.Business.Abstract.Search;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Search;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Search;

public sealed class CustomerSearchSource(IDbContextFactory<IntegrationDbContext> contextFactory) : ISearchSource
{
    public string SourceKey => "customers";
    public string Label => "Müşteriler";
    public string Icon => "ti-users";
    public string? RequiredPermission => "Permissions.Customers.View";

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var q = query.Trim();
        var pattern = $"%{q}%";

        var hits = await db.Customers
            .AsNoTracking()
            .Where(c =>
                EF.Functions.ILike(c.FullName ?? "", pattern) ||
                EF.Functions.ILike(c.Name ?? "", pattern) ||
                EF.Functions.ILike(c.Surname ?? "", pattern) ||
                EF.Functions.ILike(c.PhoneNumber ?? "", pattern))
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new SearchHitDto(
                Title: c.FullName ?? ((c.Name ?? "") + " " + (c.Surname ?? "")).Trim(),
                Subtitle: c.PhoneNumber,
                Url: $"/customers/{c.Id}",
                Score: 0.7,
                Badge: c.CustomerType))
            .ToListAsync(ct);

        return hits;
    }
}
