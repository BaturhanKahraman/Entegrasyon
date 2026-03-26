using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontSearchHistoryManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontSearchHistoryManager
{
    public async Task<IResult> RecordSearchAsync(int tenantId, string query)
    {
        var normalized = query.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return new ErrorResult("Arama sorgusu bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.StorefrontPopularSearches
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Query == normalized);

        if (existing is not null)
        {
            existing.SearchCount++;
            existing.LastSearchedAt = DateTimeOffset.UtcNow;
            dbContext.StorefrontPopularSearches.Update(existing);
        }
        else
        {
            dbContext.StorefrontPopularSearches.Add(new StorefrontPopularSearch
            {
                TenantId = tenantId,
                Query = normalized,
                SearchCount = 1,
                LastSearchedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult();
    }

    public async Task<IDataResult<List<string>>> GetPopularSearchesAsync(int tenantId, int count = 10)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var searches = await dbContext.StorefrontPopularSearches
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.SearchCount)
            .Take(count)
            .Select(s => s.Query)
            .ToListAsync();

        return new SuccessDataResult<List<string>>(searches);
    }
}
