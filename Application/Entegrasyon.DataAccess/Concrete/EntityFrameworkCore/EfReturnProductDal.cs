using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfReturnProductDal:EfEntityRepository<ReturnProduct,IntegrationDbContext>,IReturnProductDal
{
    public EfReturnProductDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}