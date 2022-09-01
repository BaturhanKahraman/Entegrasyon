using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfProductVariantDal: EfEntityRepository<ProductVariant,IntegrationDbContext>, IEfProductVariantDal
{
    public EfProductVariantDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}