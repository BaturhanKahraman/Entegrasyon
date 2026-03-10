using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract
{
    public interface IAuthService
    {
        Task<IResult> AssignTempPassword(string password, string userId, CancellationToken token = default);
        Task<IResult> CreatePassword(string password, Guid userId, CancellationToken token = default);
        Task<IResult> LoginAsync(string userName, string password);
    }
}