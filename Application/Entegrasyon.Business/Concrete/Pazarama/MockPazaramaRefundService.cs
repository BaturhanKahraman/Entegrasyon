using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama iade ve iptal servisi — mock implementasyon (test/geliştirme ortamı).
/// Gerçek API çağrısı yapmaz; listeler için boş sonuç, güncellemeler için başarı döner.
/// </summary>
public sealed class MockPazaramaRefundService(
    ILogger<MockPazaramaRefundService> logger) : IPazaramaRefundService
{
    private static readonly PazaramaRefundListResponse EmptyRefundList = new(
        ResponsePage: new PazaramaRefundPageInfo(PageSize: 100, PageIndex: 1, TotalCount: 0, TotalPages: 0),
        PageReport: new PazaramaRefundPageReport(
            TotalRefundCount: 0,
            TotalWaitingRefundCount: 0,
            TotalApprovedRefundCount: 0,
            TotalRejectedRefundCount: 0),
        RefundList: new List<PazaramaRefundDto>());

    public Task<IDataResult<PazaramaRefundListResponse>> GetRefundsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1)
    {
        logger.LogInformation("MockPazarama: GetRefunds {Start} - {End} (pageSize={PageSize}, pageNumber={PageNumber})",
            startDate, endDate, pageSize, pageNumber);
        return Task.FromResult<IDataResult<PazaramaRefundListResponse>>(
            new SuccessDataResult<PazaramaRefundListResponse>(EmptyRefundList));
    }

    public Task<IResult> UpdateRefundAsync(string refundId, int status, int? refundRejectType = null)
    {
        logger.LogInformation("MockPazarama: UpdateRefund RefundId={RefundId}, Status={Status}, RefundRejectType={RefundRejectType}",
            refundId, status, refundRejectType);
        return Task.FromResult<IResult>(new SuccessResult());
    }

    public Task<IDataResult<PazaramaRefundListResponse>> GetCancellationsAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int? refundStatus = null, int pageSize = 100, int pageNumber = 1)
    {
        logger.LogInformation("MockPazarama: GetCancellations {Start} - {End} (pageSize={PageSize}, pageNumber={PageNumber})",
            startDate, endDate, pageSize, pageNumber);
        return Task.FromResult<IDataResult<PazaramaRefundListResponse>>(
            new SuccessDataResult<PazaramaRefundListResponse>(EmptyRefundList));
    }

    public Task<IResult> UpdateCancellationAsync(string refundId, int status)
    {
        logger.LogInformation("MockPazarama: UpdateCancellation RefundId={RefundId}, Status={Status}",
            refundId, status);
        return Task.FromResult<IResult>(new SuccessResult());
    }
}
