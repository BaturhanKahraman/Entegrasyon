using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class CategoryAttributeValueManager
{
    private readonly IntegrationDbContext _ctx;
    public CategoryAttributeValueManager(IntegrationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeId(int id)
    {
        return await _ctx.CategoryAttributeValues.Where(x=>x.CategoryAttributeId== id).ToListAsync();
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeIds(IEnumerable<int> categoryAttributeIds)
    {
        return await _ctx.CategoryAttributeValues
            .Where(cav=>categoryAttributeIds.Contains(cav.CategoryAttributeId))
            .ToListAsync();
    }
}
