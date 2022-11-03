using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Shared.Middlewares;

public class MigrateDatabaseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly object _nextLock = new();
    public MigrateDatabaseMiddleware(RequestDelegate request)
    {
        _next = request;
    }

    public async Task Invoke(HttpContext context,DbContext dbContext)
    {
        var migrations = await dbContext.Database.GetPendingMigrationsAsync();
        if(migrations.Any())
        {
            await dbContext.Database.MigrateAsync();
        }
        await _next.Invoke(context);

    }
}