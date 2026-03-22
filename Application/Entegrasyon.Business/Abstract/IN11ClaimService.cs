using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IN11ClaimService
{
    // Cancel Claims (ClaimCancelService WSDL)
    Task<IDataResult<List<N11ClaimCancelDto>>> GetCancelClaimsAsync(string? status = null, int page = 0);
    Task<IResult> ApproveCancelAsync(long claimCancelId);
    Task<IResult> DenyCancelAsync(long claimCancelId, long denyReasonId, string? denyReasonNote = null);
    Task<IDataResult<List<N11ReasonTypeDto>>> GetCancelDenyReasonsAsync();

    // Return Claims (ReturnService WSDL)
    Task<IDataResult<List<N11ClaimReturnDto>>> GetReturnClaimsAsync(string? status = null, int page = 0);
    Task<IResult> ApproveReturnAsync(long claimReturnId);
    Task<IResult> DenyReturnAsync(long claimReturnId, long denyReasonId, string? denyReasonNote = null, string? returnShipmentType = null);
    Task<IResult> PendReturnAsync(long claimReturnId, long pendingReasonId, int pendingDayCount, string? pendingReasonNote = null);
    Task<IDataResult<List<N11ReasonTypeDto>>> GetReturnDenyReasonsAsync();
    Task<IDataResult<List<N11ReasonTypeDto>>> GetReturnPendingReasonsAsync();
}
