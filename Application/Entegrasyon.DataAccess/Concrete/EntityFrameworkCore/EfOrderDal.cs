using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Orders;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfOrderDal:EfEntityRepository<Order,IntegrationDbContext>,IOrderDal
{
    public EfOrderDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}