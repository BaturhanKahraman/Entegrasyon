using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface ISaleReturnManager
{
    Task<IResult> CreateReturnAsync(CreateSaleReturnDto dto);
    Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId);
    Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason);
    Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId);
}
