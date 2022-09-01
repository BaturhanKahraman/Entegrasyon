using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryAttributeDal: EfEntityRepository<CategoryAttribute,IntegrationDbContext>, ICategoryAttributeDal
{
    public EfCategoryAttributeDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}