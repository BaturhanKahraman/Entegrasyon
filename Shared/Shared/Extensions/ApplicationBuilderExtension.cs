using Microsoft.AspNetCore.Builder;
using Shared.Middlewares;

namespace Shared.Extensions;

public static class ApplicationBuilderExtension
{
    public static void AddJwtBlacklistMiddleware(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<JwtControlMiddleware>();
    }
}