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
        await dbContext.Database.MigrateAsync(context.RequestAborted);
        await _next.Invoke(context);
    }
}