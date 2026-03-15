using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Entegrasyon.Blazor.Features.Printing;

public partial class PrintStatusIndicator : IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private bool _isOnline;
    private Timer _pollTimer;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await CheckStatus();
        _pollTimer = new Timer(async _ =>
        {
            await CheckStatus();
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    private async Task CheckStatus()
    {
        try
        {
            _isOnline = await JS.InvokeAsync<bool>("PrintAgent.isAvailable");
        }
        catch
        {
            _isOnline = false;
        }
    }

    public void Dispose()
    {
        _pollTimer?.Dispose();
    }
}
