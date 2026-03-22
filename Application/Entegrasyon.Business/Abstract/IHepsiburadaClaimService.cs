using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Hepsiburada iade/değişim talepleri servisi.
/// Base URL: oms-external[-sit].hepsiburada.com
/// </summary>
public interface IHepsiburadaClaimService
{
    Task<IDataResult<List<HepsiburadaClaimDto>>> GetClaimsAsync(string? status = null);
    Task<IResult> AcceptClaimAsync(string claimNumber);
    Task<IResult> RejectClaimAsync(string claimNumber, string reason);
}
