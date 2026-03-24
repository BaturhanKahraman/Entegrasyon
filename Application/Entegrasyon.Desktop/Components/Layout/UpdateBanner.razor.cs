using Entegrasyon.Desktop.Services;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Desktop.Components.Layout;

public partial class UpdateBanner : IDisposable
{
    [Inject] private UpdateService _updateService { get; set; } = null!;

    private bool _downloading;
    private bool _downloaded;

    protected override void OnInitialized()
    {
        _updateService.OnUpdateAvailable += HandleUpdateAvailable;
    }

    private async void HandleUpdateAvailable()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task DownloadUpdate()
    {
        _downloading = true;
        await _updateService.DownloadUpdateAsync();
        _downloading = false;
        _downloaded = true;
    }

    private void RestartAndUpdate()
    {
        _updateService.ApplyUpdateAndRestart();
    }

    public void Dispose()
    {
        _updateService.OnUpdateAvailable -= HandleUpdateAvailable;
    }
}
