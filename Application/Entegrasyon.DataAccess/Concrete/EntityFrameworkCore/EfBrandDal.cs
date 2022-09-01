using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfBrandDal: EfEntityRepository<Brand,IntegrationDbContext>, IBrandDal
{
    public EfBrandDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}