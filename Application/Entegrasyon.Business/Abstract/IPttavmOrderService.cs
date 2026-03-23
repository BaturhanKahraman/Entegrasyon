using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPttavmOrderService
{
    Task<IDataResult<List<PttavmOrder>>> SearchOrdersAsync(DateTime startDate, DateTime endDate, bool isActiveOrders, CancellationToken ct = default);
    Task<IDataResult<PttavmOrderDetail>> GetOrderDetailAsync(string orderId, CancellationToken ct = default);
    Task<IDataResult<List<PttavmCargoInfo>>> GetCargoInfosAsync(string orderId, CancellationToken ct = default);
    Task<IDataResult<List<PttavmCargoProfile>>> GetCargoProfilesAsync(CancellationToken ct = default);
}
