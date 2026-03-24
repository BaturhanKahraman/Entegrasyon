using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Desktop.Components.Layout;

public partial class ConnectivityIndicator : IDisposable
{
    [Inject]
    private IConnectivity Connectivity { get; set; } = default!;

    private bool _isOnline;

    protected override void OnInitialized()
    {
        _isOnline = Connectivity.NetworkAccess == NetworkAccess.Internet;
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        _isOnline = e.NetworkAccess == NetworkAccess.Internet;
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}
