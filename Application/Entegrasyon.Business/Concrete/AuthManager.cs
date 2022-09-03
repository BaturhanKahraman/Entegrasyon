using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Auth;
using Shared.Constants;
using Shared.Results;
using Shared.Security;
using Shared.Security.Jwt;
using Shared.User;

namespace Entegrasyon.Business.Concrete;

public class AuthManager
{
    private readonly IUserManager<ApplicationUser> _userManager;
    private readonly IJwtBlackListService _jwtBlackListService;

    public AuthManager(IUserManager<ApplicationUser> userManager, IJwtBlackListService jwtBlackListService)
    {
        _userManager = userManager;
        _jwtBlackListService = jwtBlackListService;
    }
    public async Task<IResult> LoginAsync(string userName,string password)
    {
        var user =await _userManager.GetByUserName(userName);
        if (user.NeedsTakeNewPassword)
        {
            return password == user.TemporaryPassword ? 
                new SuccessDataResult<LoginNewPasswordDto>(new LoginNewPasswordDto(true)) : 
                new ErrorResult(Messages.LoginFailedWrongPassword);
        }
        var passwordValidate = HashingHelper.VerifyPasswordHash(password,user.PasswordHash,user.PasswordSalt);
        if (!passwordValidate)
            return new ErrorResult(Messages.LoginFailedWrongPassword);
        //todo
        //getdevice
        //logic runner.
        var jwtToken = user.JwtTokens.FirstOrDefault( /*device*/);
        if (jwtToken != null)
        {
            await _jwtBlackListService.AddTokenToBlackList(jwtToken.JwtToken,jwtToken.ExpiresAt.DateTime,user.Id.ToString());
            jwtToken.CurrentlyUsing = false;
        }
        
        throw new NotImplementedException();

    }

    public async Task<IResult> TakeNewPasswordAsync()
    {
        throw new NotImplementedException();
    }

    private IResult CheckIfUserActive(RootUser user)
    {
        if(!user.IsActive)
            return new ErrorResult("Hesabınız aktif değildir. Lütfen sistem yöneticisi ile irtibata geçin.");
        return new SuccessResult();
    }
}