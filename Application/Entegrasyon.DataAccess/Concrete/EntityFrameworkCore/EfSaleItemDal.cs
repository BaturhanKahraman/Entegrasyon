using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfSaleItemDal:EfEntityRepository<SaleItem,IntegrationDbContext>,ISaleItemDal
{
    public EfSaleItemDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}