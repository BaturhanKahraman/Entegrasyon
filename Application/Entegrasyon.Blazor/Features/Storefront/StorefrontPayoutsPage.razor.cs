using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontPayoutsPage
{
    [Inject] private ISellerPayoutManager PayoutManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<PayoutRequest> _payouts = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadPayoutsAsync();
    }

    private async Task LoadPayoutsAsync()
    {
        _loading = true;
        var result = await PayoutManager.GetAllPendingPayoutsAsync(1);
        if (result.Success)
            _payouts = result.Data;
        _loading = false;
    }

    private async Task ProcessAsync(PayoutRequest payout, bool approve)
    {
        var action = approve ? "onaylamak" : "reddetmek";
        var confirm = await DialogService.ShowMessageBox(
            "Odeme Talebi",
            $"{payout.Seller?.StoreName} - {payout.Amount:C2} tutarindaki talebi {action} istediginizden emin misiniz?",
            yesText: approve ? "Onayla" : "Reddet",
            cancelText: "Iptal");

        if (confirm == true)
        {
            var result = await PayoutManager.ProcessPayoutAsync(payout.Id, approve, null);
            Snackbar.Add(result.Message ?? (result.Success ? "Islendi." : "Hata."),
                result.Success ? Severity.Success : Severity.Error);
            await LoadPayoutsAsync();
        }
    }
}
