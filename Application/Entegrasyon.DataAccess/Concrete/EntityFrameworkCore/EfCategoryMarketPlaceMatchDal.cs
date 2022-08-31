using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Matches;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryMarketPlaceMatchDal:EfEntityRepository<CategoryMarketPlaceMatch,IntegrationDbContext>,ICategoryMarketPlaceMatchDal
{
    public EfCategoryMarketPlaceMatchDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}