using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Standalone stok transfer talep iş akışı (silme iş akışından bağımsız).
///
/// Lifecycle: Pending → Approved | Rejected.
/// Onay anında IOfficeStockManager.TransferStockAsync çağrılır (atomic).
/// Onaylandıktan sonra rollback yoktur. Fail durumunda talep Pending kalır (partial state yok).
/// </summary>
public interface IStockTransferRequestManager
{
    Task<IDataResult<int>> CreateAsync(
        int sourceId,
        int targetId,
        IReadOnlyList<TransferItemDto> items,
        Guid requestedBy,
        CancellationToken ct = default);

    Task<IResult> ApproveAsync(
        int requestId,
        Guid approvedBy,
        CancellationToken ct = default);

    Task<IResult> RejectAsync(
        int requestId,
        Guid rejectedBy,
        string reason,
        CancellationToken ct = default);

    Task<IDataResult<Pageable<StockTransferRequestListItemDto>>> GetPagedAsync(
        int pageIndex = 0,
        int pageSize = 50,
        StockTransferRequestStatus? status = null,
        CancellationToken ct = default);

    Task<IDataResult<StockTransferRequestDetailDto>> GetByIdAsync(
        int requestId,
        CancellationToken ct = default);

    Task<int> GetPendingCountAsync(CancellationToken ct = default);
}
