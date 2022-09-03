using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Results;

namespace Shared.User;

public class UserManager<TUser>:IUserManager<TUser>
where TUser:RootUser,new()
{
    private readonly IRandomHelper _randomHelper;
    private readonly UserContext<TUser> _userContext;
    public UserManager(IRandomHelper randomHelper, UserContext<TUser> userContext)
    {
        _randomHelper = randomHelper;
        _userContext = userContext;
    }
    public async Task<IResult> CreateUserAsync(TUser user,string password)
    {

        if (await _userContext.Users.AnyAsync(x => x.NormalizedEmail == user.NormalizedEmail))
            return new ErrorResult(Messages.LoginFailedAlreadyExists);
        await Task.CompletedTask; 
        throw new NotImplementedException();
    }

    public async Task DeactivateUserAsync(TUser user)
    {
        user.IsActive = false;
        user.IsDeleted = true;
        await _userContext.SaveChangesAsync();
    }

    public async Task CreateTemporaryPasswordAsync(TUser user)
    {
        user.TemporaryPassword = _randomHelper.GetRandomCode(15);
        _userContext.Users.Update(user);
        await _userContext.SaveChangesAsync();
    }

    public async Task<TUser> GetByEmail(string email)
    {
        email.ThrowIfNullOrEmpty();
        string normalizedEmail = email.NormalizeEmail();
        return await GetUser(x=>x.NormalizedEmail==normalizedEmail);
    }

    public async Task<TUser> GetByUserName(string userName)
    {
        userName.ThrowIfNullOrEmpty();
        return await GetUser(x => x.UserName.ToLowerInvariant() == userName.ToLowerInvariant());
    }

    private async Task<TUser> GetUser(Expression<Func<TUser,bool>> expr) =>
        await  _userContext.Users
            .Include(x=>x.JwtTokens.Where(jwt=>jwt.CurrentlyUsing && jwt.ExpiresAt>DateTimeOffset.Now))
            .FirstOrDefaultAsync(expr);
    
}