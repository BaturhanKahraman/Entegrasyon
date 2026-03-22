using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IN11OrderService
{
    Task<IDataResult<List<N11OrderDto>>> FetchOrdersAsync(
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null,
        string? status = null, int page = 0, int pageSize = 50);

    Task<IDataResult<N11OrderDto>> GetOrderDetailAsync(long orderId);

    Task<IResult> AcceptOrderItemAsync(long orderItemId, int numberOfPackages = 1);

    Task<IResult> RejectOrderItemAsync(long orderItemId, string rejectReason, string rejectReasonType);

    Task<IResult> ShipOrderItemAsync(long orderItemId, int shipmentCompanyId, string trackingNumber, int shipmentMethod = 1);
}
