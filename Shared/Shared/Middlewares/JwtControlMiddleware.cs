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
        bool hasToken = context.Request.Headers.TryGetValue("Authorization", out var token);
        if(!hasToken)
            await _next.Invoke(context);
        else
        {
            string bearerToken = token.First().Replace("Bearer ","");
            if(await blackListService.CheckBlackListToken(context.User.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)!.Value,
                   bearerToken))
                await _next.Invoke(context);
            else
            {
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("access-control-expose-headers", new StringValues("MustLogOut")); 
                context.Response.Headers.Add("MustLogOut", new StringValues("true"));
                await context.Response.WriteAsync("Başka bir cihazdan giriş yapıldı. Çıkış yapılıyor.");
            }
        }
        
    }
}