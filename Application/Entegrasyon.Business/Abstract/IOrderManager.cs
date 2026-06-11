using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Dtos.Order;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IOrderManager
{
    Task<IDataResult<Pageable<Order>>> GetOrdersAsync(OrderPaginatedRequest request);
    Task<PickingKpiDto> GetPickingKpisAsync(CancellationToken ct = default);
    Task<IDataResult<Order>> GetOrderByIdAsync(Guid orderId);
    Task<IResult> ImportTrendyolOrdersAsync(List<TrendyolShipmentPackage> packages);
    Task<IResult> ImportN11OrdersAsync(List<N11OrderDto> orders);
    Task<IResult> ImportPazaramaOrdersAsync(List<PazaramaOrderDto> orders);
    Task<IResult> UpdateOrderStatusAsync(Guid orderId, string newStatus);
    Task<IResult> UpdateOrderByShipmentPackageAsync(long shipmentPackageId, string? status, string? trackingNumber);

    // Storefront
    Task<IDataResult<List<Order>>> GetCustomerOrdersAsync(int customerId, int tenantId);
    Task<IDataResult<Order>> GetOrderDetailAsync(Guid orderId, int customerId);
    Task<IResult> CancelOrderAsync(Guid orderId, int customerId);
    Task<IDataResult<Order>> GetOrderByNumberAsync(string orderNumber);

    // Buy Again
    Task<IDataResult<List<Entegrasyon.Entity.Dtos.Storefront.StorefrontProductCardDto>>> GetPreviouslyPurchasedProductsAsync(int customerId, int count = 24);
}
