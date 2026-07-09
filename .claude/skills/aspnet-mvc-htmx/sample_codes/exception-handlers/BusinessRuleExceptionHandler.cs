// Infrastructure/ExceptionHandlers/BusinessRuleExceptionHandler.cs
// "RULE:" prefix'li InvalidOperationException'ları 422 + kullanıcı dostu mesaja çevirir.
// HTMX -> inline alert HTML; normal -> application/problem+json
using Microsoft.AspNetCore.Diagnostics;

namespace Entegrasyon.MVC.Infrastructure.ExceptionHandlers;

public class BusinessRuleExceptionHandler(
    ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is InvalidOperationException bizEx
            && bizEx.Message.StartsWith("RULE:"))
        {
            var message = bizEx.Message["RULE:".Length..].Trim();
            logger.LogWarning("Business rule violation: {Message}", message);

            httpContext.Response.StatusCode = 422;

            if (httpContext.Request.Headers.ContainsKey("HX-Request"))
            {
                httpContext.Response.ContentType = "text/html; charset=utf-8";
                await httpContext.Response.WriteAsync(
                    $"""
                    <div class="alert alert-warning alert-dismissible" role="alert">
                        {message}
                        <a class="btn-close" data-bs-dismiss="alert"></a>
                    </div>
                    """, ct);
            }
            else
            {
                httpContext.Response.ContentType = "application/problem+json";
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    title = "İş kuralı ihlali",
                    status = 422,
                    detail = message,
                    traceId = httpContext.TraceIdentifier
                }, ct);
            }
            return true;       // .NET 10: handled = true -> default diagnostics suppress edilir
        }
        return false;          // sıradaki handler'a düşür
    }
}

// ── HtmxExceptionHandler ────────────────────────────────────────────────
// HTMX request'lerinde beklenmeyen exception'ları yakalayıp inline alert HTML döner.
// Normal istekleri yakalmaz; onlar default ProblemDetails'e düşer.
//
// public class HtmxExceptionHandler(ILogger<HtmxExceptionHandler> logger) : IExceptionHandler
// {
//     public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
//     {
//         if (!ctx.Request.Headers.ContainsKey("HX-Request")) return false;
//
//         logger.LogError(ex, "HTMX request failed: {Path}", ctx.Request.Path);
//         ctx.Response.StatusCode = 500;
//         ctx.Response.ContentType = "text/html; charset=utf-8";
//         await ctx.Response.WriteAsync("""
//             <div class="alert alert-danger alert-dismissible" role="alert">
//                 <h4 class="alert-title">Bir hata oluştu</h4>
//                 <div class="text-secondary">İşleminiz gerçekleştirilemedi. Lütfen sayfayı yenileyip tekrar deneyin.</div>
//                 <a class="btn-close" data-bs-dismiss="alert"></a>
//             </div>
//             """, ct);
//         return true;
//     }
// }
//
// ── Program.cs kayıt sırası (önemli) ────────────────────────────────────
// builder.Services.AddProblemDetails();
// builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();  // önce spesifik
// builder.Services.AddExceptionHandler<HtmxExceptionHandler>();          // sonra HTMX catch-all
// // (ValidationExceptionHandler vs.)
//
// app.UseExceptionHandler();   // default ProblemDetails fallback
// app.UseStatusCodePages();
//
// .NET 10 not: TryHandleAsync true dönerse diagnostics suppress edilir.
// Geri istersen: app.UseExceptionHandler(new ExceptionHandlerOptions {
//     SuppressDiagnosticsCallback = ctx => false
// });
