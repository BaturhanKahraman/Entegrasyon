using Entegrasyon.Blazor.Services;
using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class MainLayout : IAsyncDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private IBarcodeScannerService BarcodeScannerService { get; set; } = null!;

    private MudThemeProvider _mudThemeProvider = null!;
    private LoggingErrorBoundary? _errorBoundary;
    private bool _drawerOpen = true;
    private bool _isDarkMode;
    private bool _themeLoaded;
    private MudTheme _theme = new();
    private DotNetObjectReference<MainLayout>? _dotNetRef;
    private bool _scannerInitialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var mode = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", "themeMode") ?? "system";
            _isDarkMode = mode switch
            {
                "dark" => true,
                "light" => false,
                _ => await JsRuntime.InvokeAsync<bool>("AppTheme.prefersDark")
            };
            _themeLoaded = true;

            await InitializeBarcodeScannerAsync();

            StateHasChanged();
        }
    }

    private async Task InitializeBarcodeScannerAsync()
    {
        try
        {
            var config = await BarcodeScannerService.GetConfigAsync();
            _dotNetRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("BarcodeScanner.initialize", _dotNetRef, new
            {
                enabled = config.Enabled,
                timeout = config.Timeout,
                minLength = config.MinLength
            });
            _scannerInitialized = true;
        }
        catch
        {
            // Barkod okuyucu başlatılamazsa uygulamayı etkilemesin
        }
    }

    [JSInvokable]
    public void OnBarcodeScanned(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return;

        var uri = NavigationManager.Uri;
        var relativePath = NavigationManager.ToBaseRelativePath(uri);

        if (relativePath.StartsWith("sales", StringComparison.OrdinalIgnoreCase))
        {
            // Sales sayfasındayız — query string ile yeniden yükle
            NavigationManager.NavigateTo($"/sales?barcode={Uri.EscapeDataString(barcode)}", forceLoad: false);
        }
        else if (relativePath.StartsWith("products", StringComparison.OrdinalIgnoreCase))
        {
            // Products sayfasındayız — query string ile arama tetikle
            NavigationManager.NavigateTo($"/products?barcode={Uri.EscapeDataString(barcode)}", forceLoad: false);
        }
        else
        {
            // Diğer sayfalarda — varsayılan aksiyona göre yönlendir
            _ = InvokeAsync(async () =>
            {
                var config = await BarcodeScannerService.GetConfigAsync();
                var targetUrl = config.DefaultAction switch
                {
                    "ProductSearch" => $"/products?barcode={Uri.EscapeDataString(barcode)}",
                    "NavigateToSales" => $"/sales?barcode={Uri.EscapeDataString(barcode)}",
                    _ => $"/sales?barcode={Uri.EscapeDataString(barcode)}" // SalesAdd default
                };
                NavigationManager.NavigateTo(targetUrl, forceLoad: false);
            });
        }
    }

    private async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        var mode = _isDarkMode ? "dark" : "light";
        await JsRuntime.InvokeVoidAsync("localStorage.setItem", "themeMode", mode);
        await JsRuntime.InvokeVoidAsync("AppTheme.setDataTheme", mode);
    }

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        _theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#1976D2",
                Secondary = "#424242",
                Success = "#4CAF50",
                Info = "#2196F3",
                Warning = "#FF9800",
                Error = "#F44336",
                AppbarBackground = "#1976D2",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#90CAF9",
                Secondary = "#BDBDBD",
                Success = "#81C784",
                Info = "#64B5F6",
                Warning = "#FFA726",
                Error = "#E57373",
                AppbarBackground = "#212121",
            }
        };
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        => _errorBoundary?.Recover();

    public async ValueTask DisposeAsync()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;

        if (_scannerInitialized)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("BarcodeScanner.dispose");
            }
            catch (JSDisconnectedException)
            {
                // Circuit kapatıldıysa JS çağrıları yapılamaz
            }
        }

        _dotNetRef?.Dispose();
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task HandleLogout()
    {
        if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
        {
            await customAuthStateProvider.UpdateAuthenticationState(null);
        }

        NavigationManager.NavigateTo("/auth/login", forceLoad: true);
    }
}
