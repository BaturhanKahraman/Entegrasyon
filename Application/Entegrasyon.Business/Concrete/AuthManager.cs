using Entegrasyon.Entity;
using Shared.Constants;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;
using Shared.Security;
using Shared.Security.Jwt;
using Shared.User;
using Shared.User.Dto;
using Shared.User.Services;

namespace Entegrasyon.Business.Concrete;

public class AuthManager
{
    private readonly IUserManager<ApplicationUser> _userManager;
    private readonly IJwtBlackListService _jwtBlackListService;
    private readonly Microsoft.AspNetCore.Http.HttpContext _httpContext;
    private readonly ITokenHelper _tokenHelper;
    private readonly ILoginManager<ApplicationUser> _loginManager;

    public AuthManager(IUserManager<ApplicationUser> userManager, IJwtBlackListService jwtBlackListService, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor, ITokenHelper tokenHelper, ILoginManager<ApplicationUser> loginManager)
    {
        _userManager = userManager;
        _jwtBlackListService = jwtBlackListService;
        _tokenHelper = tokenHelper;
        _loginManager = loginManager;
        _httpContext = httpContextAccessor.HttpContext;
    }

    public async Task<IResult> LoginAsync(string userName,string password)
    {
        return await _loginManager.LoginWithUserNameAsync(userName, password);
    }
    public async Task<IResult> TakeNewPasswordAsync(string password,string userId)
    {
        await _userManager.CreateUserPasswordAsync(password,userId);
        return new SuccessResult(Messages.FirstPasswordAssigned);
    }

    public async Task<IResult> LogOut(string userId)
    {
        var user = await _userManager.GetUserAsync(x => x.Id == Guid.Parse(userId),false);
        if (_httpContext.IsMobileDevice())
        {
            user.MobileJwtToken=string.Empty;
            user.MobileJwtTokenExpiresAt=DateTimeOffset.MinValue;
        }
        else
        {
            user.WebJwtToken = string.Empty;
            user.WebJwtTokenExpiresAt=DateTimeOffset.MinValue;
        }

        await _userManager.UpdateUser(user);
        return new SuccessResult(Messages.LogOut);
    }
}