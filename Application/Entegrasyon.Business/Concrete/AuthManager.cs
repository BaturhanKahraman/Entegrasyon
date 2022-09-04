using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Auth;
using Microsoft.AspNetCore.Http;
using Shared.Constants;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;
using Shared.Security;
using Shared.Security.Jwt;
using Shared.User;
using Shared.User.Services;

namespace Entegrasyon.Business.Concrete;

public class AuthManager
{
    private readonly IUserManager<ApplicationUser> _userManager;
    private readonly IJwtBlackListService _jwtBlackListService;
    private readonly HttpContext _httpContext;
    private readonly ITokenHelper _tokenHelper;

    public AuthManager(IUserManager<ApplicationUser> userManager, IJwtBlackListService jwtBlackListService, IHttpContextAccessor httpContextAccessor, ITokenHelper tokenHelper)
    {
        _userManager = userManager;
        _jwtBlackListService = jwtBlackListService;
        _tokenHelper = tokenHelper;
        _httpContext = httpContextAccessor.HttpContext;
    }
    // TODO
    // SIGNINMANAGER 
    public async Task<IResult> LoginAsync(string userName,string password)
    {
        var user = await _userManager.GetByUserName(userName);
        if (user==null)
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        if (user.NeedsTakeNewPassword)
        {
            if(password == user.TemporaryPassword)
                return new SuccessDataResult<LoginNewPasswordDto>(new LoginNewPasswordDto(true,user.Id.ToString()),"Şifre alma sayfasına yönlendiriliyorsunuz.");
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        }
        var result =LogicRunner.Run(
            (ValidatePassword(password,user),1),
                (CheckIfUserActive(user),2));
        if(result != null)
            return result;

        bool isMobile= _httpContext.IsMobileDevice();
        var jwtToken = new AccessToken()
        {
            ExpiresAt = isMobile ? user.MobileJwtTokenExpiresAt.DateTime : user.WebJwtTokenExpiresAt.DateTime,
            Token = isMobile ? user.MobileJwtToken : user.WebJwtToken
        };
        if(jwtToken.ExpiresAt>DateTime.Now)
            await _jwtBlackListService.AddTokenToBlackList(jwtToken.Token,jwtToken.ExpiresAt,user.Id.ToString());
        var newToken = _tokenHelper.CreateToken(user);
        if (isMobile){
            user.MobileJwtToken=newToken.Token;
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
    public async Task<IResult> TakeNewPasswordAsync(string password,string userId)
    {
        await _userManager.CreateUserPassword(password,userId);
        return new SuccessResult(Messages.FirstPasswordAssigned);
    }

    private static IResult ValidatePassword(string password,ApplicationUser user)
    {
        var passwordValidate = HashingHelper.VerifyPasswordHash(password,user.PasswordHash,user.PasswordSalt);
        if(!passwordValidate)
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        return new SuccessResult();
    }
    
    private IResult CheckIfUserActive(RootUser user)
    {
        if(!user.IsActive)
            return new ErrorResult("Hesabınız aktif değildir. Lütfen sistem yöneticisi ile irtibata geçin.");
        return new SuccessResult();
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