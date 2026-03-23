using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICiceksepetiOrderService
{
    Task<IDataResult<CiceksepetiOrderListResponse>> GetOrdersAsync(CiceksepetiGetOrdersRequest request, CancellationToken ct = default);
    Task<IResult> ReadyForCargoWithCsAsync(CiceksepetiCsCargoRequest request, CancellationToken ct = default);
    Task<IResult> UpdateStatusWithOwnCargoAsync(CiceksepetiOwnCargoRequest request, CancellationToken ct = default);
    Task<IResult> ChangeCargoCompanyAsync(CiceksepetiChangeCargoRequest request, CancellationToken ct = default);
    Task<IResult> SendCargoMeasurementAsync(CiceksepetiCargoMeasurementRequest request, CancellationToken ct = default);
    Task<IResult> SendDigitalCodeAsync(CiceksepetiDigitalCodeRequest request, CancellationToken ct = default);
    Task<IResult> UpdateLaborCostAsync(CiceksepetiLaborCostRequest request, CancellationToken ct = default);
}
