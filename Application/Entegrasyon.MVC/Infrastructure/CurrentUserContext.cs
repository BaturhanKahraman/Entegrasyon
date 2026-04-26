using System.Security.Claims;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.MVC.Infrastructure;

public sealed class CurrentUserContext(IHttpContextAccessor http) : ICurrentUserContext
{
    public Guid? UserId
    {
        get
        {
            var val = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }
}
