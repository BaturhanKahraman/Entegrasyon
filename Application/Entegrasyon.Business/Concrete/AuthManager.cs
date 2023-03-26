using Entegrasyon.Entity;
using FluentValidation;
using Shared.Constants;
using Shared.Extensions;
using Shared.Results;
using Shared.Security.Jwt;
using Shared.User;
using Shared.User.Services;

namespace Entegrasyon.Business.Concrete;

public class AuthManager
{
    private readonly IUserManager<ApplicationUser> _userManager;
    private readonly Microsoft.AspNetCore.Http.HttpContext _httpContext;
    private readonly ITokenHelper _tokenHelper;
    private readonly ILoginManager<ApplicationUser> _loginManager;

    public AuthManager(IUserManager<ApplicationUser> userManager,Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,ITokenHelper tokenHelper,ILoginManager<ApplicationUser> loginManager)
    {
        _userManager = userManager;
        _tokenHelper = tokenHelper;
        _loginManager = loginManager;
        _httpContext = httpContextAccessor.HttpContext;
    }


    public async Task<IDataResult<AccessToken>> LoginJWTAsync(string userName,string password)
    {
        var result = await LoginAsync(userName,password);
        var res = result as SuccessDataResult<ApplicationUser>;
        var token = _tokenHelper.CreateToken(res!.Data);
        return new SuccessDataResult<AccessToken>(token);
    }

    public async Task<IResult> LoginAsync(string userName,string password)
    {
        var result = await _loginManager.LoginWithUserNameAsync(userName,password);
        if(result is not SuccessDataResult<RootUser> res)
            return result;
        var appUser = res.Data as ApplicationUser;
        return new SuccessDataResult<ApplicationUser>(appUser);
    }
    public async Task<IResult> AssignNewPassword(string password,string userId)
    {
        var user = await _userManager.GetUserAsync(x => x.Id == Guid.Parse(userId),true);
        if(user != null)
        {
            user.NeedsTakeNewPassword = true;
            user.TemporaryPassword = password;
            await _userManager.UpdateUser(user);
            return new SuccessResult(Messages.TemporaryPasswordAssigned);
        }
        return new ErrorResult(Messages.Failed);
    }

    public async Task<IResult> CreatePassword(string password,Guid userId)
    {
        if(string.IsNullOrEmpty(password))
        {
            throw new ValidationException("Eksik bilgi");
        }
        if(!await _userManager.Exits(x => x.Id == userId))
            return new ErrorResult(Messages.UserNotFound);
        await _userManager.CreateUserPasswordAsync(password,userId.ToString());
        return new SuccessResult(Messages.FirstPasswordAssigned);
    }

    public async Task<IResult> LogOut(string userId)
    {
        var user = await _userManager.GetUserAsync(x => x.Id == Guid.Parse(userId),false);
        if(_httpContext.IsMobileDevice())
        {
            user.MobileJwtToken = string.Empty;
            user.MobileJwtTokenExpiresAt = DateTimeOffset.MinValue;
        }
        else
        {
            user.WebJwtToken = string.Empty;
            user.WebJwtTokenExpiresAt = DateTimeOffset.MinValue;
        }

        await _userManager.UpdateUser(user);
        return new SuccessResult(Messages.LogOut);
    }
}