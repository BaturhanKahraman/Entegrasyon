using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryAttributeValueDal: EfEntityRepository<CategoryAttributeValue,IntegrationDbContext>, ICategoryAttributeValueDal
{
    public EfCategoryAttributeValueDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}