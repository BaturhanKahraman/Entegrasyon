using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;
namespace Entegrasyon.Business.Abstract;
public interface ICiceksepetiReturnService
{
    Task<IDataResult<CiceksepetiReturnListResponse>> GetReturnOrdersAsync(CiceksepetiGetReturnsRequest request, CancellationToken ct = default);
    Task<IResult> ConfirmReturnReceivedAsync(CiceksepetiReturnReceivedRequest request, CancellationToken ct = default);
    Task<IResult> EvaluateReturnAsync(CiceksepetiReturnEvaluationRequest request, CancellationToken ct = default);
}
