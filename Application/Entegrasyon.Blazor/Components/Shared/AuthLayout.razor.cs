using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class AuthLayout
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private bool _isDarkMode;

    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976D2",
            Secondary = "#424242",
            Success = "#4CAF50",
            Info = "#2196F3",
            Warning = "#FF9800",
            Error = "#F44336",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90CAF9",
            Secondary = "#BDBDBD",
            Success = "#81C784",
            Info = "#64B5F6",
            Warning = "#FFA726",
            Error = "#E57373",
        }
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var mode = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", "themeMode") ?? "system";
            _isDarkMode = mode switch
            {
                "dark" => true,
                "light" => false,
                _ => await JsRuntime.InvokeAsync<bool>("eval",
                    "window.matchMedia('(prefers-color-scheme: dark)').matches")
            };
            StateHasChanged();
        }
    }
}
