using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.POS;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPOSSessionManager
{
    Task<IDataResult<POSSession>> OpenSessionAsync(OpenSessionDto dto);
    Task<IResult> CloseSessionAsync(CloseSessionDto dto);
    Task<IDataResult<POSSession>> GetActiveSessionAsync(int branchOfficeId, string? terminalId = null);
    Task<IDataResult<POSTransaction>> RecordTransactionAsync(POSTransactionDto dto);
    Task<IResult> AddCashMovementAsync(AddCashMovementDto dto);
    Task<IDataResult<POSSummaryDto>> GetSessionSummaryAsync(long sessionId);
    Task<IDataResult<POSSummaryDto>> GetDailySummaryAsync(int branchOfficeId, DateOnly date);
}
