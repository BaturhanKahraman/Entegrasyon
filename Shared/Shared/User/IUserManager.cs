using Shared.Results;

namespace Shared.User;

public interface IUserManager<TUser>
    where TUser : RootUser, new()
{
    Task<TUser> GetByEmail(string email);
    Task<IResult> CreateUserAsync(TUser user,string password);
    Task DeactivateUserAsync(TUser user);
    Task CreateTemporaryPasswordAsync(TUser user);
    Task<TUser> GetByUserName(string userName);
}