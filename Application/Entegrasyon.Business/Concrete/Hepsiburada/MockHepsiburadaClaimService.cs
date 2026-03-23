using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Mock Hepsiburada iade/değişim servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; başarılı sonuçlar döndürür.
/// </summary>
public sealed class MockHepsiburadaClaimService(
    ILogger<MockHepsiburadaClaimService> logger) : IHepsiburadaClaimService
{
    public Task<IDataResult<List<HepsiburadaClaimDto>>> GetClaimsAsync(string? status = null)
    {
        logger.LogInformation("[MOCK] HB get claims: Status={Status}", status);
        return Task.FromResult<IDataResult<List<HepsiburadaClaimDto>>>(
            new SuccessDataResult<List<HepsiburadaClaimDto>>([]));
    }

    public Task<IResult> AcceptClaimAsync(string claimNumber)
    {
        logger.LogInformation("[MOCK] HB accept claim: {ClaimNumber}", claimNumber);
        return Task.FromResult<IResult>(new SuccessResult("İade talebi onaylandı (MOCK)."));
    }

    public Task<IResult> RejectClaimAsync(string claimNumber, string reason)
    {
        logger.LogInformation("[MOCK] HB reject claim: {ClaimNumber}, Reason={Reason}", claimNumber, reason);
        return Task.FromResult<IResult>(new SuccessResult("İade talebi reddedildi (MOCK)."));
    }
}
