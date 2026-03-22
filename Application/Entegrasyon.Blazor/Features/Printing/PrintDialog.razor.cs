using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.PrintAgent.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Printing;

public partial class PrintDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public PrintJobDto PrintJob { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _loading = true;
    private bool _agentAvailable;
    private bool _printing;
    private string _selectedPrinter = string.Empty;
    private int _copies = 1;
    private string _fallbackMode = "browser";
    private List<DiscoveredPrinter> _printers = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _agentAvailable = await JS.InvokeAsync<bool>("PrintAgent.isAvailable");

        if (_agentAvailable)
        {
            var printers = await JS.InvokeAsync<DiscoveredPrinter[]>("PrintAgent.getPrinters");
            _printers = printers?.ToList() ?? [];
            _selectedPrinter = _printers.FirstOrDefault(p => p.IsDefault)?.Name
                            ?? _printers.FirstOrDefault()?.Name
                            ?? string.Empty;
        }

        _loading = false;
        StateHasChanged();
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task PrintViaAgent()
    {
        if (string.IsNullOrEmpty(_selectedPrinter)) return;

        _printing = true;
        StateHasChanged();

        try
        {
            var rawBytesBase64 = PrintJob.RawBytes is { Length: > 0 }
                ? Convert.ToBase64String(PrintJob.RawBytes)
                : null;

            var result = await JS.InvokeAsync<PrintJobResult>("PrintAgent.print",
                PrintJob.ZplContent,
                rawBytesBase64,
                _selectedPrinter,
                _copies,
                PrintJob.PrinterLanguage);

            if (result?.Status?.StartsWith("error") == true)
            {
                Snackbar.Add($"Yazdırma hatası: {result.Status}", Severity.Error);
            }
            else
            {
                Snackbar.Add("Yazdırma komutu gönderildi", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Yazdırma hatası: {ex.Message}", Severity.Error);
        }
        finally
        {
            _printing = false;
        }
    }

    private async Task HandleFallback()
    {
        if (_fallbackMode == "browser")
        {
            var html = BuildPrintHtml();
            await JS.InvokeVoidAsync("PrintAgent.browserPrint", html);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            if (!string.IsNullOrEmpty(PrintJob.ZplContent))
            {
                await JS.InvokeVoidAsync("PrintAgent.downloadFile",
                    PrintJob.ZplContent, "etiket.zpl", "text/plain");
            }
            else if (PrintJob.RawBytes is { Length: > 0 })
            {
                var base64 = Convert.ToBase64String(PrintJob.RawBytes);
                await JS.InvokeVoidAsync("PrintAgent.downloadFile",
                    base64, "fis.bin", "application/octet-stream");
            }

            Snackbar.Add("Dosya indirildi", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    private string BuildPrintHtml()
    {
        // ZPL içeriğinden basit HTML etiket oluştur
        if (string.IsNullOrEmpty(PrintJob.ZplContent))
            return "<p>Önizleme mevcut değil (ESC/POS formatı)</p>";

        // ZPL'den field data'ları parse et
        var lines = new List<string>();
        var content = PrintJob.ZplContent;
        var fdStart = 0;
        while ((fdStart = content.IndexOf("^FD", fdStart, StringComparison.Ordinal)) >= 0)
        {
            fdStart += 3;
            var fdEnd = content.IndexOf("^FS", fdStart, StringComparison.Ordinal);
            if (fdEnd > fdStart)
                lines.Add(content[fdStart..fdEnd]);
        }

        var html = "<div style='font-family:monospace;padding:10mm;border:1px dashed #ccc;width:80mm;'>";
        foreach (var line in lines)
        {
            html += $"<p style='margin:4px 0;'>{System.Net.WebUtility.HtmlEncode(line)}</p>";
        }
        html += "</div>";

        return html;
    }

    private record PrintJobResult(string JobId, string Status);
}
