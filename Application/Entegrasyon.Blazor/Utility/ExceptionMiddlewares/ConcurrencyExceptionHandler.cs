using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Blazor.Utility.ExceptionMiddlewares;

public class ConcurrencyExceptionHandler(ILogger logger) : IExceptionHandler
{
    private readonly ILogger _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
            return false;
        _logger.LogError(exception, "Eşzamanlılık hatası: kayıt başka bir işlem tarafından değiştirilmiş.");
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Eşzamanlılık Hatası",
            Detail = "Bu kayıt başka bir işlem tarafından güncellenmiş. Lütfen sayfayı yenileyip tekrar deneyin."
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}