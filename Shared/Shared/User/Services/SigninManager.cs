using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.User.Services;

public class SigninManager<TUser>:ISigninManager
where TUser:RootUser
{
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public SigninManager(ILogger logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
}