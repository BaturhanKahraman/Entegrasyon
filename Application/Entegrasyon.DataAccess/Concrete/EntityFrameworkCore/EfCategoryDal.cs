using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryDal: EfEntityRepository<Category,IntegrationDbContext>,ICategoryDal
{
    private readonly IntegrationDbContext _context;
    public EfCategoryDal(IntegrationDbContext ctx) : base(ctx)
    {
        _context = ctx;
    }

    public async Task UpdateRangeAsync(IEnumerable<Category> categories)
    {
        _context.Categories.UpdateRange(categories);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}