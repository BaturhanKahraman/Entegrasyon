using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontReturnsPage
{
    [Inject] private IStorefrontReturnManager ReturnManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<StorefrontReturnRequest> _returns = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadReturnsAsync();
    }

    private async Task LoadReturnsAsync()
    {
        _loading = true;
        var result = await ReturnManager.GetAllReturnsAsync(1);
        if (result.Success)
            _returns = result.Data;
        _loading = false;
    }

    private async Task OpenApproveDialog(StorefrontReturnRequest returnRequest)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Iade Onayla",
            $"Iade talebini onaylamak istediginizden emin misiniz?\nNeden: {returnRequest.Reason}",
            yesText: "Onayla",
            cancelText: "Iptal");

        if (confirm == true)
        {
            var result = await ReturnManager.UpdateReturnStatusAsync(
                returnRequest.Id, ReturnStatus.Approved, "Iade talebi onaylandi.", null);
            Snackbar.Add(result.Message ?? "Onaylandi.", result.Success ? Severity.Success : Severity.Error);
            await LoadReturnsAsync();
        }
    }

    private async Task OpenRejectDialog(StorefrontReturnRequest returnRequest)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Iade Reddet",
            $"Iade talebini reddetmek istediginizden emin misiniz?\nNeden: {returnRequest.Reason}",
            yesText: "Reddet",
            cancelText: "Iptal");

        if (confirm == true)
        {
            var result = await ReturnManager.UpdateReturnStatusAsync(
                returnRequest.Id, ReturnStatus.Rejected, "Iade talebi reddedildi.", null);
            Snackbar.Add(result.Message ?? "Reddedildi.", result.Success ? Severity.Success : Severity.Error);
            await LoadReturnsAsync();
        }
    }
}
