using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Logic;
using Shared.Results;
using Shared.Security;

namespace Shared.User.Services;

public class UserManager<TUser> : IUserManager<TUser>
where TUser : RootUser, new()
{
    private readonly IRandomGenerator _randomGenerator;
    private readonly UserContext<TUser> _userContext;
    public UserManager(IRandomGenerator randomGenerator, UserContext<TUser> userContext)
    {
        _randomGenerator = randomGenerator;
        _userContext = userContext;
    }
    public async Task<IResult> CreateUserAsync(TUser user, string password)
    {
        AssignPassword(user, password);
        return await CreateUserAsync(user);
    }

    public async Task<IResult> CreateUserAsync(TUser user)
    {
        var badResult = LogicRunner.Run(await CheckIfTheSameUserExits(user));
        if (badResult != null)
            return badResult;
        if (!string.IsNullOrEmpty(user.Email))
        {
            user.NormalizedEmail = user.Email.NormalizeEmail();
        }
        await _userContext.Users.AddAsync(user);
        await _userContext.SaveChangesAsync();
        return new SuccessResult();
    }

    public async Task DeactivateUserAsync(TUser user)
    {
        user.IsActive = false;
        user.IsDeleted = true;
        _userContext.Users.Update(user);
        await _userContext.SaveChangesAsync();
    }

    public async Task CreateTemporaryPasswordAsync(TUser user)
    {
        user.TemporaryPassword = _randomGenerator.GetRandomCode(15);
        _userContext.Users.Update(user);
        await _userContext.SaveChangesAsync();
    }

    public async Task<TUser> GetByEmail(string email)
    {
        email.ThrowIfNullOrEmpty();
        string normalizedEmail = email.NormalizeEmail();
        return await GetUser(x => x.NormalizedEmail == normalizedEmail,false);
    }

    public async Task<TUser> GetByUserName(string userName)
    {
        userName.ThrowIfNullOrEmpty();
        return await GetUser(x => string.Equals(x.UserName, userName),false);
    }

    public async Task CreateUserPassword(string password, string userId)
    {
        var user = await GetUser(x=>x.Id==Guid.Parse(userId),true);
        AssignPassword(user, password);
        user.NeedsTakeNewPassword = false;
        _userContext.Users.Update(user);
        await _userContext.SaveChangesAsync();
    }

    public async Task UpdateUser(TUser user)
    {
        _userContext.Users.Update(user);
        await _userContext.SaveChangesAsync();
    }

    private static void AssignPassword(TUser user, string password)
    {
        HashingHelper.CreatePasswordHash(password, out var pHash, out var pSalt);
        user.PasswordHash = pHash;
        user.PasswordSalt = pSalt;
    }

    private async Task<IResult> CheckIfTheSameUserExits(TUser user)
    {
        if (await _userContext.Users.AnyAsync(x => string.Equals(x.NormalizedEmail, user.Email.NormalizeEmail())) ||
           await _userContext.Users.AnyAsync(u => string.Equals(u.UserName, user.UserName)))
            return new ErrorResult(Messages.LoginFailedAlreadyExists);
        return new SuccessResult();
    }
    public  Task<TUser> GetUser(Expression<Func<TUser, bool>> expr,bool isTracking) =>
         isTracking? _userContext.Users.FirstOrDefaultAsync(expr) : _userContext.Users.AsNoTracking().FirstOrDefaultAsync(expr);

}