using Entegrasyon.Entity;
using Shared.Results;
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
        throw new NotImplementedException();

    }

    private IResult CheckIfUserActive(RootUser user)
    {
        if(!user.IsActive)
            return new ErrorResult("Hesabınız aktif değildir. Lütfen sistem yöneticisi ile irtibata geçin.");
        return new SuccessResult();
    }
}