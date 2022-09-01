using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryDal: EfEntityRepository<Category,IntegrationDbContext>,ICategoryDal
{
    public EfCategoryDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}