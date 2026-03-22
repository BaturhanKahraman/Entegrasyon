using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAmazonOrderService
{
    Task<IDataResult<List<AmazonOrderDto>>> GetOrdersAsync(
        DateTimeOffset createdAfter, string[] marketplaceIds,
        string[]? orderStatuses = null, CancellationToken ct = default);
    Task<IDataResult<AmazonOrderDto>> GetOrderAsync(string orderId, CancellationToken ct = default);
    Task<IDataResult<List<AmazonOrderItemDto>>> GetOrderItemsAsync(string orderId, CancellationToken ct = default);
    Task<IResult> ConfirmShipmentAsync(string orderId, AmazonConfirmShipmentRequest request, CancellationToken ct = default);
}
