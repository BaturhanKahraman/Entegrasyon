using Entegrasyon.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class MainLayout : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    private MudThemeProvider _mudThemeProvider = null!;
    private LoggingErrorBoundary? _errorBoundary;
    private bool _drawerOpen = true;
    private bool _isDarkMode;
    private MudTheme _theme = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var stored = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", "darkMode");
            if (stored is not null)
            {
                _isDarkMode = stored == "true";
            }
            else
            {
                _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
            }
            StateHasChanged();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await JsRuntime.InvokeVoidAsync("localStorage.setItem", "darkMode", _isDarkMode.ToString().ToLower());
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

    public void Dispose()
        => NavigationManager.LocationChanged -= OnLocationChanged;

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
