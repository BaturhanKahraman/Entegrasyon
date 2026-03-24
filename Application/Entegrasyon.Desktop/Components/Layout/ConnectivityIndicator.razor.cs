using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Desktop.Components.Layout;

public partial class ConnectivityIndicator : IDisposable
{
    private bool _isOnline;
    private Timer? _timer;

    protected override void OnInitialized()
    {
        // Check immediately, then every 10 seconds
        _ = CheckConnectivity();
        _timer = new Timer(async _ => await CheckConnectivity(), null,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    private async Task CheckConnectivity()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            await http.GetAsync("https://www.google.com/generate_204");
            _isOnline = true;
        }
        catch
        {
            _isOnline = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
