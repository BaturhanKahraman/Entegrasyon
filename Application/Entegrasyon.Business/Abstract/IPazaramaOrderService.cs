using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaOrderService
{
    Task<IDataResult<List<PazaramaOrderDto>>> FetchOrdersAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int pageSize = 500, int pageNumber = 1);
    Task<IResult> UpdateOrderItemStatusAsync(long orderNumber, PazaramaOrderItemUpdate item);
    Task<IResult> BulkUpdateOrderStatusAsync(long orderNumber, int status);
}
