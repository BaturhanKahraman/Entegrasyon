using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryAttributeCategoryDal:EfEntityRepository<CategoryAttributeCategory,IntegrationDbContext>,ICategoryAttributeCategoryDal
{
    private readonly IntegrationDbContext _dbContext;
    public EfCategoryAttributeCategoryDal(IntegrationDbContext ctx) : base(ctx)
    {
        _dbContext = ctx;
    }

    public async Task UpdateRangeAsync(IEnumerable<CategoryAttributeCategory> cacList)
    {
        _dbContext.CategoryAttributeCategories.UpdateRange(cacList);
        await _dbContext.SaveChangesAsync();
    }
}