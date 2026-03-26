using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontSellersPage
{
    [Inject] private ISellerManager SellerManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<Seller> _sellers = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadSellersAsync();
    }

    private async Task LoadSellersAsync()
    {
        _loading = true;
        var result = await SellerManager.GetAllSellersAsync(1);
        if (result.Success)
            _sellers = result.Data;
        _loading = false;
    }

    private async Task ApproveAsync(Seller seller)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Satici Onayla",
            $"{seller.StoreName} adli saticiyi onaylamak istediginizden emin misiniz?",
            yesText: "Onayla",
            cancelText: "Iptal");

        if (confirm == true)
        {
            var result = await SellerManager.ApproveSellerAsync(seller.Id);
            Snackbar.Add(result.Message ?? (result.Success ? "Onaylandi." : "Hata."),
                result.Success ? Severity.Success : Severity.Error);
            await LoadSellersAsync();
        }
    }

    private async Task OpenRejectDialog(Seller seller)
    {
        var result = await DialogService.ShowMessageBox(
            "Satici Reddet",
            $"{seller.StoreName} adli saticiyi reddetmek istediginizden emin misiniz?",
            yesText: "Reddet",
            cancelText: "Iptal");

        if (result == true)
        {
            var rejectResult = await SellerManager.RejectSellerAsync(seller.Id, "Yonetici tarafindan reddedildi.");
            Snackbar.Add(rejectResult.Message ?? (rejectResult.Success ? "Reddedildi." : "Hata."),
                rejectResult.Success ? Severity.Success : Severity.Error);
            await LoadSellersAsync();
        }
    }

    private async Task OpenSuspendDialog(Seller seller)
    {
        var result = await DialogService.ShowMessageBox(
            "Satici Askiya Al",
            $"{seller.StoreName} adli saticiyi askiya almak istediginizden emin misiniz?",
            yesText: "Askiya Al",
            cancelText: "Iptal");

        if (result == true)
        {
            var suspendResult = await SellerManager.SuspendSellerAsync(seller.Id, "Yonetici tarafindan askiya alindi.");
            Snackbar.Add(suspendResult.Message ?? (suspendResult.Success ? "Askiya alindi." : "Hata."),
                suspendResult.Success ? Severity.Success : Severity.Error);
            await LoadSellersAsync();
        }
    }
}
