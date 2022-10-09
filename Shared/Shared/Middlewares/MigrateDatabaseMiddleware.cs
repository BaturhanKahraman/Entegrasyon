using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Shared.Middlewares;

public class MigrateDatabaseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public MigrateDatabaseMiddleware(RequestDelegate request,ILogger<ExceptionMiddleware> logger)
    {
        _next = request;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context,DbContext dbContext)
    {
        var migrations = await dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
        if (migrations.Any())
        {
            await dbContext.Database.MigrateAsync(context.RequestAborted).ConfigureAwait(false);
        }
        await _next.Invoke(context);
    }
}