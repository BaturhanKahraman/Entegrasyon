using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;
using Shared.Security;
using Shared.Security.Jwt;
using Shared.User.Dto;

namespace Shared.User.Services;

public class LoginManager<TUser,TLogin,TContext>:ILoginManager<TUser>
where TUser:RootUser
where TContext:DbContext
where TLogin:RootLogin,new()
{
    private readonly ILogger _logger;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
    private readonly IUserManager<TUser> _userManager;
    private readonly IJwtBlackListService _jwtBlackListService;
    private readonly ITokenHelper _tokenHelper;
    private readonly TContext _tContext;
    public LoginManager(ILogger<LoginManager<TUser,TLogin,TContext>> logger,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor, IUserManager<TUser> userManager,
        IJwtBlackListService blackListService, ITokenHelper tokenHelper,
        TContext tContext)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _jwtBlackListService = blackListService;
        _tokenHelper = tokenHelper;
        _tContext = tContext;
    }

    public async Task<IResult> LoginWithUserNameAsync(string userName,string password)
    {
        _logger.LogInformation("Login");
        userName = userName.Normalize();
        var user = await _userManager.GetUserFullInformation(x=>x.NormalizedUserName==userName).ConfigureAwait(false);
        if(user == null)
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        if(user.NeedsTakeNewPassword)
        {
            if(password == user.TemporaryPassword)
                return new SuccessDataResult<LoginNewPasswordDto>(new LoginNewPasswordDto(true,user.Id.ToString()),"Şifre alma sayfasına yönlendiriliyorsunuz.");
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        }
        var result = LogicRunner.Run(
            (ValidatePassword(password,user), 1),
            (CheckIfUserActive(user), 2));
        if(result != null)
            return result;
        await AddLoginRecord(user.Id);
        return new SuccessDataResult<RootUser>(user);
        bool isMobile = _httpContextAccessor.HttpContext.IsMobileDevice();
        var jwtToken = new AccessToken()
        {
            ExpiresAt = isMobile ? user.MobileJwtTokenExpiresAt.DateTime : user.WebJwtTokenExpiresAt.DateTime,
            Token = isMobile ? user.MobileJwtToken : user.WebJwtToken
        };
        //if(jwtToken.ExpiresAt > DateTime.Now)
            //await _jwtBlackListService.AddTokenToBlackList(jwtToken.Token,jwtToken.ExpiresAt,user.Id.ToString());
        var newToken = _tokenHelper.CreateToken(user);
        if(isMobile)
        {
            user.MobileJwtToken = newToken.Token;
            user.MobileJwtTokenExpiresAt = newToken.ExpiresAt;
        }
        else
        {
            user.WebJwtToken = newToken.Token;
            user.WebJwtTokenExpiresAt = newToken.ExpiresAt;
        }
        await _userManager.UpdateUser(user);
        return new SuccessDataResult<AccessToken>(newToken);
    }

    public async Task AddLoginRecord(Guid userId)
    {
        string ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        var login = new TLogin()
        {
            IpAddress = ipAddress, LoginTime = DateTimeOffset.UtcNow, RootUserId =userId
        };
        await _tContext.Set<TLogin>().AddAsync(login,_httpContextAccessor.HttpContext.RequestAborted).ConfigureAwait(false);
        await _tContext.SaveChangesAsync(_httpContextAccessor.HttpContext.RequestAborted).ConfigureAwait(false);
    }

    public async Task<IResult> LogOutAsync(Guid userId)
    {
        _logger.LogInformation("Logout request came. userid : {0}",userId);
        var user = await _userManager.GetUserAsync(x => x.Id == userId,false);
        if(_httpContextAccessor.HttpContext.IsMobileDevice())
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

    private static IResult ValidatePassword(string password,TUser user)
    {
        var passwordValidate = HashingHelper.VerifyPasswordHash(password,user.PasswordHash,user.PasswordSalt);
        if(!passwordValidate)
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        return new SuccessResult();
    }

    private IResult CheckIfUserActive(RootUser user)
    {
        if(!user.IsActive)
            return new ErrorResult(Messages.AccountNotActive);
        return new SuccessResult();
    }
}