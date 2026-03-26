using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ISellerOrderManager
{
    Task<IDataResult<List<Order>>> GetSellerOrdersAsync(int sellerId);
    Task<IDataResult<Order>> GetSellerOrderDetailAsync(int sellerId, Guid orderId);
    Task<IResult> UpdateSellerOrderStatusAsync(int sellerId, Guid orderId, string newStatus);
}
