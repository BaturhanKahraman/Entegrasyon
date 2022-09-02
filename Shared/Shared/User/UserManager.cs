using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Results;

namespace Shared.User;

public class UserManager:IUserManager
{
    private readonly IRandomHelper _randomHelper;
    private readonly UserContext<RootUser> _userContext;
    public UserManager(IRandomHelper randomHelper, UserContext<RootUser> userContext)
    {
        _randomHelper = randomHelper;
        _userContext = userContext;
    }
    public async Task<IResult> CreateUserAsync(RootUser user,string password)
    {

        if (await _userContext.Users.AnyAsync(x => x.NormalizedEmail == user.NormalizedEmail))
            return new ErrorResult(Messages.LoginFailedAlreadyExists);
        await Task.CompletedTask; 
        throw new NotImplementedException();
    }

    public async Task DeactivateUserAsync(RootUser user)
    {
        user.IsActive = false;
        user.IsDeleted = true;
        await _userContext.SaveChangesAsync();
    }

    public async Task CreateTemporaryPasswordAsync(RootUser user)
    {
        user.TemporaryPassword = _randomHelper.GetRandomCode(15);
        _userContext.Users.Update(user);
        await _userContext.SaveChangesAsync();
    }

}