using Entegrasyon.PrintAgent.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings;

public partial class PrinterSettings : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _loading = true;
    private bool _agentOnline;
    private bool _testing;
    private bool? _connectionResult;
    private int _wizardStep;

    private string _agentUrl = "https://localhost:19100";
    private string _agentApiKey = string.Empty;

    private List<DiscoveredPrinter> _printers = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await RefreshStatus();
        _loading = false;
        StateHasChanged();
    }

    private async Task RefreshStatus()
    {
        _agentOnline = await JS.InvokeAsync<bool>("PrintAgent.isAvailable");

        if (_agentOnline)
        {
            var printers = await JS.InvokeAsync<DiscoveredPrinter[]>("PrintAgent.getPrinters");
            _printers = printers?.ToList() ?? [];
        }
        else
        {
            _printers = [];
        }
    }

    private async Task TestConnection()
    {
        _testing = true;
        _connectionResult = null;
        StateHasChanged();

        try
        {
            await JS.InvokeVoidAsync("PrintAgent.configure", _agentUrl, _agentApiKey);
            _connectionResult = await JS.InvokeAsync<bool>("PrintAgent.isAvailable");

            if (_connectionResult == true)
            {
                await RefreshStatus();
                Snackbar.Add("Agent bağlantısı başarılı!", Severity.Success);
            }
            else
            {
                Snackbar.Add("Agent'a bağlanılamadı", Severity.Error);
            }
        }
        catch
        {
            _connectionResult = false;
            Snackbar.Add("Bağlantı hatası", Severity.Error);
        }
        finally
        {
            _testing = false;
        }
    }

    private async Task TestPrint(string printerName)
    {
        var testZpl = """
            ^XA
            ^FO50,50^A0N,30,30^FDTest Etiketi^FS
            ^FO50,100^A0N,20,20^FDEntegrasyon Print Agent^FS
            ^FO50,140^BCN,60,Y,N,N^FD1234567890^FS
            ^FO50,240^A0N,24,24^FDBu bir test baskisidir^FS
            ^XZ
            """;

        var result = await JS.InvokeAsync<PrintTestResult>("PrintAgent.print",
            testZpl, null, printerName, 1, "ZPL");

        if (result?.Status?.StartsWith("error") == true)
            Snackbar.Add($"Test başarısız: {result.Status}", Severity.Error);
        else
            Snackbar.Add($"Test etiketi gönderildi: {printerName}", Severity.Success);
    }

    private record PrintTestResult(string JobId, string Status);
}
