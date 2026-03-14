using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Entegrasyon.Blazor.Features.Products;

public partial class SyncWarningBanner
{
    [Parameter, EditorRequired] public Guid ProductId { get; set; }
    [Parameter, EditorRequired] public List<OutOfSyncMarketplaceInfo> OutOfSyncMarketplaces { get; set; } = [];
    [Parameter, EditorRequired] public DateTimeOffset ProductUpdatedAt { get; set; }
    [Parameter] public EventCallback OnSyncRequested { get; set; }

    [Inject] private IJSRuntime Js { get; set; } = null!;

    private bool _dismissed;

    private string LocalStorageKey => $"sync-dismissed-{ProductId}-{ProductUpdatedAt.Ticks}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && OutOfSyncMarketplaces.Count > 0)
        {
            var stored = await Js.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
            if (stored is not null)
            {
                _dismissed = true;
                StateHasChanged();
            }
        }
    }

    private async Task SyncNow()
    {
        await OnSyncRequested.InvokeAsync();
        _dismissed = true;
    }

    private void RemindLater()
    {
        _dismissed = true;
    }

    private async Task Dismiss()
    {
        _dismissed = true;
        await Js.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, "true");
    }
}

public sealed record OutOfSyncMarketplaceInfo(int MarketPlaceId, string Name);
