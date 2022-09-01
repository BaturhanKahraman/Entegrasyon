using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfMainProductDal: EfEntityRepository<MainProduct,IntegrationDbContext>, IMainProductDal
{
    public EfMainProductDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}