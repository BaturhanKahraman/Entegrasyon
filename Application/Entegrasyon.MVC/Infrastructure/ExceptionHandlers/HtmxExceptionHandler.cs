using Microsoft.AspNetCore.Diagnostics;

namespace Entegrasyon.MVC.Infrastructure.ExceptionHandlers;

/// <summary>
/// HTMX isteklerinde beklenmeyen hataları yakalar ve inline hata mesajı döner.
/// Normal istekleri yakalmaz — onlar ProblemDetails fallback'e düşer.
/// </summary>
public class HtmxExceptionHandler(
    ILogger<HtmxExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (!httpContext.Request.Headers.ContainsKey("HX-Request"))
            return false;

        logger.LogError(exception, "HTMX request failed: {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = 500;
        httpContext.Response.ContentType = "text/html; charset=utf-8";

        await httpContext.Response.WriteAsync("""
            <div class="alert alert-danger alert-dismissible" role="alert">
                <div class="d-flex">
                    <div>
                        <h4 class="alert-title">Bir hata oluştu</h4>
                        <div class="text-secondary">İşleminiz gerçekleştirilemedi. Lütfen sayfayı yenileyip tekrar deneyin.</div>
                    </div>
                </div>
                <a class="btn-close" data-bs-dismiss="alert" aria-label="Close"></a>
            </div>
            """, ct);

        return true;
    }
}
