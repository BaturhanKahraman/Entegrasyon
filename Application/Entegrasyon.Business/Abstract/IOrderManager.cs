using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IOrderManager
{
    Task<IDataResult<List<Order>>> GetOrdersAsync(int? marketPlaceId = null, int page = 0, int pageSize = 50);
    Task<IDataResult<Order>> GetOrderByIdAsync(Guid orderId);
    Task<IResult> ImportTrendyolOrdersAsync(List<TrendyolShipmentPackage> packages);
    Task<IResult> UpdateOrderStatusAsync(Guid orderId, string newStatus);
    Task<IResult> UpdateOrderByShipmentPackageAsync(long shipmentPackageId, string? status, string? trackingNumber);
}
