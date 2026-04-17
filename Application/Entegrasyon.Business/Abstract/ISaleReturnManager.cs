using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface ISaleReturnManager
{
    Task<IResult> CreateReturnAsync(CreateSaleReturnDto dto);
    Task<IResult> UpdateReturnAsync(UpdateSaleReturnDto dto);
    Task<IResult> SubmitReturnAsync(long returnId, Guid userId);
    Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId);
    Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason);
    Task<IResult> CancelReturnAsync(CancelSaleReturnDto dto);
    Task<IResult> CompleteReturnAsync(CompleteSaleReturnDto dto);
    Task<IResult> RestoreItemToStockAsync(long saleReturnItemId, Guid userId, int branchOfficeId);
    Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId);
}
