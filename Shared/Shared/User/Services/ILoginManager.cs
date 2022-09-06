using Shared.Results;

namespace Shared.User.Services;

public interface ILoginManager<T>
{
    Task<IResult> LoginWithUserNameAsync(string userName,string password);
    Task AddLoginRecord(Guid userId);
    Task<IResult> LogOutAsync(Guid userGuid);
}