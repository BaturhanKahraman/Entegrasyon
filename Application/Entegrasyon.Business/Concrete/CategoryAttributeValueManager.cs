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
        using var dbContext = _contextFactory.CreateDbContext();
        return await dbContext.CategoryAttributeValues.Where(x=>x.CategoryAttributeId== id).ToListAsync();
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeIds(IEnumerable<int> categoryAttributeIds)
    {
        using var dbContext = _contextFactory.CreateDbContext();
        return await dbContext.CategoryAttributeValues
            .Where(cav=>categoryAttributeIds.Contains(cav.CategoryAttributeId))
            .ToListAsync();
    }
}
