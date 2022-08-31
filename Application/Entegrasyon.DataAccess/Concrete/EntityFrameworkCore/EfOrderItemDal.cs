using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Orders;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfOrderItemDal:EfEntityRepository<OrderItem,IntegrationDbContext>,IOrderItemDal
{
    public EfOrderItemDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}