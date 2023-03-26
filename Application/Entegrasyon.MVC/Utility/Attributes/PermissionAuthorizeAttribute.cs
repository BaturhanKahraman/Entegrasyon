using Entegrasyon.MVC.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.MVC.Utility.Attributes;

public class PermissionAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string[] _permissions;

    public PermissionAuthorizeAttribute(string permissions)
    {
        _permissions = permissions.Trim(' ').Split(',');
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity!.IsAuthenticated)
        {
            var permissions = user.Claims.Where(c => c.Type == StringConstant.Permission).Select(c=>c.Value).ToList();
            if (permissions.Any(p => _permissions.Contains(p,StringComparer.InvariantCultureIgnoreCase)))
                return;
            context.Result = new ForbidResult();
        }
    }
}