using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontMessagesPage
{
    [Inject] private IStorefrontContactManager ContactManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<StorefrontContactMessage> _messages = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadMessagesAsync();
    }

    private async Task LoadMessagesAsync()
    {
        _loading = true;
        var result = await ContactManager.GetMessagesAsync(1);
        if (result.Success)
            _messages = result.Data;
        _loading = false;
    }

    private async Task MarkAsReadAsync(StorefrontContactMessage message)
    {
        var result = await ContactManager.MarkAsReadAsync(message.Id);
        Snackbar.Add(result.Message ?? (result.Success ? "Okundu." : "Hata."),
            result.Success ? Severity.Success : Severity.Error);
        await LoadMessagesAsync();
    }
}
