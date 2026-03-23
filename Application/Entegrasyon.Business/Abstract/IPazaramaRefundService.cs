using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaRefundService
{
    Task<IDataResult<PazaramaRefundListResponse>> GetRefundsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1);
    Task<IResult> UpdateRefundAsync(string refundId, int status, int? refundRejectType = null);
    Task<IDataResult<PazaramaRefundListResponse>> GetCancellationsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1);
    Task<IResult> UpdateCancellationAsync(string refundId, int status);
}
