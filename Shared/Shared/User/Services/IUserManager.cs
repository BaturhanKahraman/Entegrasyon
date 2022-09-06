using System.Linq.Expressions;
using Shared.Results;

namespace Shared.User.Services;

public interface IUserManager<TUser>
    where TUser : RootUser
{
    Task<TUser> GetByEmail(string email);
    Task<IResult> CreateUserAsync(TUser user, string password);
    Task DeactivateUserAsync(TUser user);
    Task CreateTemporaryPasswordAsync(TUser user);
    Task<TUser> GetByUserName(string userName);
    Task<IResult> CreateUserAsync(TUser user);
    Task<IResult> CreateUserPasswordAsync(string password, string userId);
    Task UpdateUser(TUser user);
    Task<TUser> GetUserAsync(Expression<Func<TUser, bool>> expr,bool isTracking);
    Task<TUser> GetUserFullInformation(Expression<Func<TUser,bool>> expr);
    Task<List<TUser>> GetUsers();
}