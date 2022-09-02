using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Shared.Security.Jwt;

namespace Shared.Middlewares;

public class JwtControlMiddleware
{
    private readonly RequestDelegate _next;

    public JwtControlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,IJwtBlackListService blackListService)
    {
        if(!context.Request.Headers.TryGetValue("Authorization",out _))
            await _next.Invoke(context);
        if(await blackListService.CheckBlackListToken(context.User.FindFirst(x=>x.Type==ClaimTypes.NameIdentifier)!.Value))
            await _next.Invoke(context);
        else
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Add("MustLogOut",new StringValues("true"));
        }
    }
}