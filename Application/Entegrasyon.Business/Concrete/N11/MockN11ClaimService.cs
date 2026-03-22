using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// Mock N11 talep servisi — boş listeler ve başarı sonuçları döner.
/// Gerçek SOAP bağlantısı olmadan UI geliştirmesi için kullanılır.
/// </summary>
public sealed class MockN11ClaimService(
    ILogger<MockN11ClaimService> logger) : IN11ClaimService
{
    // -----------------------------------------------------------------------
    // Cancel Claims
    // -----------------------------------------------------------------------

    public Task<IDataResult<List<N11ClaimCancelDto>>> GetCancelClaimsAsync(string? status = null, int page = 0)
    {
        logger.LogInformation(
            "Mock N11: GetCancelClaimsAsync çağrıldı — status={Status}, page={Page}",
            status, page);

        return Task.FromResult<IDataResult<List<N11ClaimCancelDto>>>(
            new SuccessDataResult<List<N11ClaimCancelDto>>([], "N11 iptal talep listesi (mock — boş)."));
    }

    public Task<IResult> ApproveCancelAsync(long claimCancelId)
    {
        logger.LogInformation(
            "Mock N11: ApproveCancelAsync çağrıldı — claimCancelId={ClaimCancelId}", claimCancelId);

        return Task.FromResult<IResult>(new SuccessResult("Mock: iptal talebi onaylandı."));
    }

    public Task<IResult> DenyCancelAsync(long claimCancelId, long denyReasonId, string? denyReasonNote = null)
    {
        logger.LogInformation(
            "Mock N11: DenyCancelAsync çağrıldı — claimCancelId={ClaimCancelId}, denyReasonId={DenyReasonId}, denyReasonNote={DenyReasonNote}",
            claimCancelId, denyReasonId, denyReasonNote);

        return Task.FromResult<IResult>(new SuccessResult("Mock: iptal talebi reddedildi."));
    }

    public Task<IDataResult<List<N11ReasonTypeDto>>> GetCancelDenyReasonsAsync()
    {
        logger.LogInformation("Mock N11: GetCancelDenyReasonsAsync çağrıldı");

        return Task.FromResult<IDataResult<List<N11ReasonTypeDto>>>(
            new SuccessDataResult<List<N11ReasonTypeDto>>([], "N11 iptal red nedenleri (mock — boş)."));
    }

    // -----------------------------------------------------------------------
    // Return Claims
    // -----------------------------------------------------------------------

    public Task<IDataResult<List<N11ClaimReturnDto>>> GetReturnClaimsAsync(string? status = null, int page = 0)
    {
        logger.LogInformation(
            "Mock N11: GetReturnClaimsAsync çağrıldı — status={Status}, page={Page}",
            status, page);

        return Task.FromResult<IDataResult<List<N11ClaimReturnDto>>>(
            new SuccessDataResult<List<N11ClaimReturnDto>>([], "N11 iade talep listesi (mock — boş)."));
    }

    public Task<IResult> ApproveReturnAsync(long claimReturnId)
    {
        logger.LogInformation(
            "Mock N11: ApproveReturnAsync çağrıldı — claimReturnId={ClaimReturnId}", claimReturnId);

        return Task.FromResult<IResult>(new SuccessResult("Mock: iade talebi onaylandı."));
    }

    public Task<IResult> DenyReturnAsync(long claimReturnId, long denyReasonId, string? denyReasonNote = null, string? returnShipmentType = null)
    {
        logger.LogInformation(
            "Mock N11: DenyReturnAsync çağrıldı — claimReturnId={ClaimReturnId}, denyReasonId={DenyReasonId}, denyReasonNote={DenyReasonNote}, returnShipmentType={ReturnShipmentType}",
            claimReturnId, denyReasonId, denyReasonNote, returnShipmentType);

        return Task.FromResult<IResult>(new SuccessResult("Mock: iade talebi reddedildi."));
    }

    public Task<IResult> PendReturnAsync(long claimReturnId, long pendingReasonId, int pendingDayCount, string? pendingReasonNote = null)
    {
        logger.LogInformation(
            "Mock N11: PendReturnAsync çağrıldı — claimReturnId={ClaimReturnId}, pendingReasonId={PendingReasonId}, pendingDayCount={PendingDayCount}, pendingReasonNote={PendingReasonNote}",
            claimReturnId, pendingReasonId, pendingDayCount, pendingReasonNote);

        return Task.FromResult<IResult>(new SuccessResult("Mock: iade talebi beklemeye alındı."));
    }

    public Task<IDataResult<List<N11ReasonTypeDto>>> GetReturnDenyReasonsAsync()
    {
        logger.LogInformation("Mock N11: GetReturnDenyReasonsAsync çağrıldı");

        return Task.FromResult<IDataResult<List<N11ReasonTypeDto>>>(
            new SuccessDataResult<List<N11ReasonTypeDto>>([], "N11 iade red nedenleri (mock — boş)."));
    }

    public Task<IDataResult<List<N11ReasonTypeDto>>> GetReturnPendingReasonsAsync()
    {
        logger.LogInformation("Mock N11: GetReturnPendingReasonsAsync çağrıldı");

        return Task.FromResult<IDataResult<List<N11ReasonTypeDto>>>(
            new SuccessDataResult<List<N11ReasonTypeDto>>([], "N11 iade bekleme nedenleri (mock — boş)."));
    }
}
