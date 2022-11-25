using Entegrasyon.Entity.Orders;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface IOrderItemDal : IEntityRepository<OrderItem>
{
}