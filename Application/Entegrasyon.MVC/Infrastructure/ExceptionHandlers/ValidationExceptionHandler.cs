using System.Net;
using Entegrasyon.MVC.Infrastructure.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.MVC.Infrastructure.ExceptionHandlers;

/// <summary>
/// FluentValidation.ValidationException'ı yakalar ve dostça Türkçe mesaja çevirir.
/// Birçok business manager doğrulamayı ValidateAndThrowAsync ile fırlatıyor; controller'lar
/// yakalamadığı için bu hatalar HTTP 500'e dönüşüyordu (Bug #3/#5). Bu handler onları:
///  - HTMX isteklerinde: showToast (danger) + 422
///  - normal isteklerde: TempData toast + referer'a redirect (PRG); olmazsa dostça HTML sayfası
/// olarak ele alır. Zincirin EN BAŞINDA kayıtlı olmalı.
/// </summary>
public class ValidationExceptionHandler(
    ITempDataDictionaryFactory tempDataFactory,
    ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception exception, CancellationToken ct)
    {
        if (exception is not ValidationException vex)
            return false;

        var message = string.Join(" • ", vex.Errors
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct());
        if (string.IsNullOrWhiteSpace(message))
            message = "Lütfen girdiğiniz bilgileri kontrol edin.";

        logger.LogWarning("Doğrulama hatası: {Path} — {Message}", ctx.Request.Path, message);

        if (ctx.Response.HasStarted)
            return false;

        // HTMX: mevcut showToast konvansiyonu
        if (ctx.Request.Headers.ContainsKey("HX-Request"))
        {
            ctx.Response.HtmxTriggerWithData("showToast", new { message, type = "danger" });
            ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return true;
        }

        // Normal POST: TempData toast + referer'a redirect (PRG). Exception bağlamında
        // TempData/session kırılgan olabildiği için try/catch ile dostça HTML'e düşeriz.
        try
        {
            var tempData = tempDataFactory.GetTempData(ctx);
            tempData.SetError(message);
            tempData.Save();

            var back = ctx.Request.Headers.Referer.FirstOrDefault();
            ctx.Response.Redirect(string.IsNullOrWhiteSpace(back) ? "/" : back);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TempData/redirect başarısız, dostça HTML'e düşülüyor.");
            if (ctx.Response.HasStarted) return true;
            ctx.Response.Clear();
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            var safe = WebUtility.HtmlEncode(message);
            await ctx.Response.WriteAsync($$"""
                <!DOCTYPE html><html lang="tr"><head><meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Doğrulama Hatası</title></head>
                <body style="font-family:system-ui,sans-serif;max-width:560px;margin:64px auto;padding:24px;color:#1f2937">
                <h2 style="margin:0 0 12px">İşlem tamamlanamadı</h2>
                <p style="background:#fef2f2;border:1px solid #fecaca;color:#991b1b;padding:12px 14px;border-radius:8px">{{safe}}</p>
                <p><a href="javascript:history.back()" style="color:#2563eb">← Geri dön ve düzeltin</a></p>
                </body></html>
                """, ct);
            return true;
        }
    }
}
