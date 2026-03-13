using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public class LoggingErrorBoundary : ErrorBoundary
{
    [Inject] private ILogger<LoggingErrorBoundary> Logger { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Unhandled exception caught by ErrorBoundary: {Message}", exception.Message);
        Snackbar.Add("Beklenmeyen bir hata oluştu. Lütfen sayfayı yenileyin veya farklı bir sayfaya geçin.", Severity.Error);
        return Task.CompletedTask;
    }
}
