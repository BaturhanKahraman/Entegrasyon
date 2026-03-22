using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IN11OrderService
{
    Task<IDataResult<List<N11OrderDto>>> FetchOrdersAsync(
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null,
        string? status = null, int page = 0, int pageSize = 50);

    Task<IDataResult<N11OrderDto>> GetOrderDetailAsync(long orderId);
}
