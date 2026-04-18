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

    /// <summary>
    /// Var olan bir Sale için POSSession ↔ Sale bağlantı satırı oluşturur.
    /// MakeSale'i çağırmaz — sadece POSTransaction kaydı yazar.
    /// POSController.CompleteSale akışında, MakeSale tamamlandıktan sonra çağrılır;
    /// böylece GetSessionSummary totalCash/expectedCash doğru hesaplanır.
    /// </summary>
    Task<IResult> AddTransactionRecordAsync(
        long sessionId,
        Guid saleId,
        decimal cashReceived,
        decimal changeGiven);
    Task<IResult> AddCashMovementAsync(AddCashMovementDto dto);
    Task<IDataResult<POSSummaryDto>> GetSessionSummaryAsync(long sessionId);
    Task<IDataResult<POSSummaryDto>> GetDailySummaryAsync(int branchOfficeId, DateOnly date);
    Task<IDataResult<POSReportDto>> GetXReportAsync(long sessionId);
    Task<IDataResult<POSReportDto>> GetZReportAsync(long sessionId);
}
