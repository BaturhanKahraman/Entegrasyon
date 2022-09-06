using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Extensions;

public static class HttpContextExtension
{
    public static bool IsMobileDevice(this HttpContext context)
    {
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var isMobile = userAgent.Contains("Android") || userAgent.Contains("iPhone");
        return isMobile;
    }

    public static string GetUserId(this HttpContext context)=>
        context.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)!.Value;
    
    // ReSharper disable once InconsistentNaming
    public static string GetIPAddress(this HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();
}