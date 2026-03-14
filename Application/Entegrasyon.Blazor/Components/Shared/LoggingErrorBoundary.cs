using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public class LoggingErrorBoundary : ErrorBoundary
{
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
        Snackbar.Add(message, Severity.Error);
        return Task.CompletedTask;
    }
}
