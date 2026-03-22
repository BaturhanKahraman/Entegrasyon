using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Entegrasyon.Blazor.Features.Printing;

public partial class PrintStatusIndicator : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private bool _isOnline;
    private Timer? _pollTimer;
    private CancellationTokenSource _cts = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await CheckStatus();
        _pollTimer = new Timer(async _ =>
        {
            if (_cts.IsCancellationRequested) return;

            await CheckStatus();

            if (_cts.IsCancellationRequested) return;

            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    private async Task CheckStatus()
    {
        try
        {
            _isOnline = await JS.InvokeAsync<bool>("PrintAgent.isAvailable", _cts.Token);
        }
        catch
        {
            _isOnline = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        if (_pollTimer is not null)
            await _pollTimer.DisposeAsync();

        _cts.Dispose();
    }
}
