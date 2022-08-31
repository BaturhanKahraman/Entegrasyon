using Entegrasyon.Entity.Orders;
using Shared.Abstract;

namespace Entegrasyon.DataAccess.Abstract;

public interface IOrderItemDal : IEntityRepository<OrderItem>
{
}