using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public class LoggingErrorBoundary : ErrorBoundary
{
    private static readonly ActivitySource BlazorActivitySource = new("Entegrasyon.Blazor");

    [Inject] private ILogger<LoggingErrorBoundary> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    protected override Task OnErrorAsync(Exception exception)
    {
        var message = exception switch
        {
            DbUpdateConcurrencyException =>
                "Bu kayıt başka bir işlem tarafından güncellenmiş. Lütfen sayfayı yenileyip tekrar deneyin.",
            InvalidOperationException ex when ex.Message.Contains("Yetersiz stok") =>
                "Stok yetersiz — başka bir satış bu ürünün stokunu tüketmiş olabilir.",
            _ => "Beklenmeyen bir hata oluştu. Lütfen sayfayı yenileyin."
        };

        Logger.LogError(exception, "ErrorBoundary: {Message}", exception.Message);

        // OpenTelemetry trace — Blazor circuit hatalarını dashboard'da göster
        using var activity = BlazorActivitySource.StartActivity("Blazor.ErrorBoundary", ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.SetTag("error.type", exception.GetType().Name);
            activity.SetTag("error.message", exception.Message);
            activity.SetTag("error.user_message", message);
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                ["exception.type"] = exception.GetType().FullName,
                ["exception.message"] = exception.Message,
                ["exception.stacktrace"] = exception.StackTrace ?? ""
            }));
        }

        Snackbar.Add(message, Severity.Error);
        return Task.CompletedTask;
    }
}
