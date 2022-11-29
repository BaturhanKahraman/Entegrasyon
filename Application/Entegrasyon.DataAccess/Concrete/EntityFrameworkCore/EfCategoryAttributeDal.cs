using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryAttributeDal: EfEntityRepository<CategoryAttribute,IntegrationDbContext>, ICategoryAttributeDal
{
    private readonly IntegrationDbContext _integrationDbContext;
    public EfCategoryAttributeDal(IntegrationDbContext ctx) : base(ctx)
    {
        _integrationDbContext = ctx;    
    }

    public async Task AddRangeAsync(IEnumerable<CategoryAttribute> categoryAttr)
    {
        await _integrationDbContext.AddRangeAsync(categoryAttr);
        await _integrationDbContext.SaveChangesAsync();
    }
}