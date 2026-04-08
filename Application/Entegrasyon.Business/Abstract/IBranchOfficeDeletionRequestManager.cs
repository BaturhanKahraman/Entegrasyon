using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Şube ofisi silme talep iş akışı:
///   RequestDelete → Pending → Approve (Stage A transfer + Stage B soft-delete) | Reject
/// Stok transferi `IOfficeStockManager.TransferStockAsync` üzerinden yeniden kullanılır.
/// Stage B, "son aktif ofis" race'i için pg advisory lock altında serialized çalışır.
/// </summary>
public interface IBranchOfficeDeletionRequestManager
{
    Task<IDataResult<int>> RequestDeleteAsync(
        int branchOfficeId,
        Guid requestedBy,
        int? targetBranchOfficeId,
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

    Task<IDataResult<Pageable<BranchOfficeDeletionRequestListDto>>> GetPagedAsync(
        int pageIndex = 0,
        int pageSize = 50,
        BranchOfficeDeletionRequestStatus? status = null,
        CancellationToken ct = default);

    Task<IDataResult<BranchOfficeDeletionRequestDetailDto>> GetByIdAsync(
        int requestId,
        CancellationToken ct = default);

    Task<int> GetPendingCountAsync(CancellationToken ct = default);
}
