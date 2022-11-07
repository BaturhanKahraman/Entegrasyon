using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Logic;
using Shared.Results;
using Shared.Security;

namespace Shared.User.Services;
//IOptions ile optionslar getir.
public class UserManager<TUser,TContext> : IUserManager<TUser>
where TUser : RootUser
where TContext : DbContext
{
    private readonly IRandomGenerator _randomGenerator;
    private readonly TContext _context;
    public UserManager(IRandomGenerator randomGenerator,TContext context)
    {
        _randomGenerator = randomGenerator;
        _context = context;
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
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.CreatedAt = DateTimeOffset.UtcNow;
        await _context.Set<TUser>().AddAsync(user).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return new SuccessResult();
    }

    public async Task DeactivateUserAsync(TUser user)
    {
        user.IsActive = false;
        user.IsDeleted = true;
        _context.Set<TUser>().Update(user);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task CreateTemporaryPasswordAsync(TUser user)
    {
        user.TemporaryPassword = _randomGenerator.GetRandomCode(15);
        _context.Set<TUser>().Update(user);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<TUser> GetByEmail(string email)
    {
        email.ThrowIfNullOrEmpty();
        string normalizedEmail = email.NormalizeEmail();
        return await GetUserAsync(x => x.NormalizedEmail == normalizedEmail,false).ConfigureAwait(false);
    }

    public async Task<TUser> GetByUserName(string userName)
    {
        userName.ThrowIfNullOrEmpty();
        userName = userName.ToUpperInvariant();
        return await GetUserAsync(x => x.NormalizedUserName == userName,false);
    }

    public async Task<IResult> CreateUserPasswordAsync(string password, string userId)
    {
        var user = await GetUserAsync(x=>x.Id==Guid.Parse(userId),true);
        if(!user.NeedsTakeNewPassword)
            return new ErrorResult(Messages.CantChangePassword);
        AssignPassword(user, password);
        user.NeedsTakeNewPassword = false;
        _context.Set<TUser>().Update(user);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return new SuccessResult(Messages.PasswordCreated);
    }

    public async Task UpdateUser(TUser user)
    {
        _context.Set<TUser>().Update(user);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    private static void AssignPassword(TUser user, string password)
    {
        HashingHelper.CreatePasswordHash(password, out var pHash, out var pSalt);
        user.PasswordHash = pHash;
        user.PasswordSalt = pSalt;
    }

    private async Task<IResult> CheckIfTheSameUserExits(TUser user)
    {
        if (await _context.Set<TUser>().AnyAsync(x => string.Equals(x.NormalizedEmail, user.Email.NormalizeEmail())) ||
           await _context.Set<TUser>().AnyAsync(u => string.Equals(u.UserName, user.UserName)))
            return new ErrorResult(Messages.RegisterFailedAlreadyExists);
        return new SuccessResult();
    }
    public  Task<TUser> GetUserAsync(Expression<Func<TUser, bool>> expr,bool isTracking) =>
         isTracking? _context.Set<TUser>().FirstOrDefaultAsync(expr) : _context.Set<TUser>().AsNoTracking().FirstOrDefaultAsync(expr);
    public Task<TUser> GetUserFullInformation(Expression<Func<TUser,bool>> expr) =>
         _context.Set<TUser>()
             .Include(x=>x.Logins)
             .Include(x=>x.Role)
                .ThenInclude(x=>x.Claims)
             .FirstOrDefaultAsync(expr);

    public Task<List<TUser>> GetUsers() => _context.Set<TUser>().AsNoTracking().ToListAsync();
}