using Microsoft.AspNetCore.Diagnostics;

namespace Entegrasyon.MVC.Infrastructure.ExceptionHandlers;

/// <summary>
/// İş kuralı ihlallerini yakalar ve kullanıcı dostu mesaj döner.
/// HTMX isteklerinde inline alert partial, normal isteklerde ProblemDetails.
/// </summary>
public class BusinessRuleExceptionHandler(
    ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        // BusinessRuleException varsa yakala
        // TODO: BusinessRuleException entity'de tanımlandığında burayı güncelle
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
                    $"<div class=\"alert alert-warning alert-dismissible\" role=\"alert\">{message}" +
                    "<a class=\"btn-close\" data-bs-dismiss=\"alert\"></a></div>", ct);
            }
            else
            {
                httpContext.Response.ContentType = "application/problem+json";
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    title = "Is kurali ihlali",
                    status = 422,
                    detail = message,
                    traceId = httpContext.TraceIdentifier
                }, ct);
            }

            return true;
        }

        return false;
    }
}
