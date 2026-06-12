using Entegrasyon.Business.Helpers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public sealed class CategoryAttributeValueManager : ICategoryAttributeValueManager
{
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    public CategoryAttributeValueManager(IDbContextFactory<IntegrationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeId(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.CategoryAttributeValues.Where(x=>x.CategoryAttributeId== id).ToListAsync();
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeIds(IEnumerable<int> categoryAttributeIds)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.CategoryAttributeValues
            .Where(cav=>categoryAttributeIds.Contains(cav.CategoryAttributeId))
            .ToListAsync();
    }

    public async Task<int> GetOrCreate(int categoryAttributeId, string rawName)
    {
        var normalized = AttributeValueNormalizer.Normalize(rawName);
        if (normalized.Length == 0)
            throw new ArgumentException("Değer boş olamaz.", nameof(rawName));

        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var existing = await dbContext.CategoryAttributeValues
            .Where(v => v.CategoryAttributeId == categoryAttributeId && v.NormalizedName == normalized && !v.IsDeleted)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();
        if (existing.HasValue)
            return existing.Value;

        var entity = new CategoryAttributeValue
        {
            CategoryAttributeId = categoryAttributeId,
            Name = rawName.Trim(),
            NormalizedName = normalized
        };
        dbContext.CategoryAttributeValues.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync();
            return entity.Id;
        }
        catch (DbUpdateException)
        {
            // Race: another request inserted the same canonical value -> unique index violation. Return existing id.
            await using var retry = await _contextFactory.CreateDbContextAsync();
            return await retry.CategoryAttributeValues
                .Where(v => v.CategoryAttributeId == categoryAttributeId && v.NormalizedName == normalized && !v.IsDeleted)
                .Select(v => v.Id)
                .FirstAsync();
        }
    }
}
