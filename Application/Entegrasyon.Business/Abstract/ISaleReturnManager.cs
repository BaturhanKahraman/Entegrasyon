using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface ISaleReturnManager
{
    Task<IDataResult<long>> CreateReturnAsync(CreateSaleReturnDto dto);
    Task<IResult> UpdateReturnAsync(UpdateSaleReturnDto dto);
    Task<IResult> SubmitReturnAsync(long returnId, Guid userId);
    Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId);
    Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason);
    Task<IResult> CancelReturnAsync(CancelSaleReturnDto dto);
    Task<IResult> CompleteReturnAsync(CompleteSaleReturnDto dto);
    Task<IResult> RestoreItemToStockAsync(long saleReturnItemId, Guid userId, int branchOfficeId);
    Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId);

    /// <summary>
    /// İade liste sayfası üst KPI kartları için global snapshot (toplam/Pending/Approved
    /// sayıları + Completed RefundAmount toplamı). Tek GroupBy(ReturnStatus), N+1 yok.
    /// </summary>
    Task<ReturnKpiDto> GetReturnKpisAsync(CancellationToken ct = default);
}
