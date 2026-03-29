using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class UnsavedChangesGuard : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter] public bool IsDirty { get; set; }

    [Parameter] public string Message { get; set; } =
        "Kaydedilmemiş değişiklikleriniz var. Sayfadan ayrılmak istediğinize emin misiniz?";

    private bool _previousIsDirty;

    protected override async Task OnParametersSetAsync()
    {
        if (IsDirty != _previousIsDirty)
        {
            _previousIsDirty = IsDirty;
            if (IsDirty)
                await JsRuntime.InvokeVoidAsync("unsavedChangesInterop.addBeforeUnloadListener");
            else
                await JsRuntime.InvokeVoidAsync("unsavedChangesInterop.removeBeforeUnloadListener");
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (!IsDirty) return;

        var confirmed = await DialogService.ShowMessageBox(
            "Kaydedilmemiş Değişiklikler",
            Message,
            yesText: "Evet, Ayrıl",
            noText: "Hayır");

        if (confirmed != true)
        {
            context.PreventNavigation();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("unsavedChangesInterop.removeBeforeUnloadListener");
        }
        catch (JSDisconnectedException)
        {
            // Circuit already disconnected
        }
    }
}
