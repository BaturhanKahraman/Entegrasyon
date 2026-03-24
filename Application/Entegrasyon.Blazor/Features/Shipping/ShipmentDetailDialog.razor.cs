using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Shipping;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Shipping;

public partial class ShipmentDetailDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public ShipmentTrackingDto? Shipment { get; set; }

    [Inject]
    private IShipmentTrackingManager ShipmentTrackingManager { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private bool _isRefreshing;

    private async Task OnRefreshClicked()
    {
        if (Shipment == null) return;

        _isRefreshing = true;
        try
        {
            var result = await ShipmentTrackingManager.RefreshTrackingStatusAsync(Shipment.Id);
            if (result.Success)
            {
                Snackbar.Add("Kargo durumu guncellendi", Severity.Success);

                // Reload history
                var historyResult = await ShipmentTrackingManager.GetShipmentHistoryAsync(Shipment.Id);
                if (historyResult.Success)
                {
                    Shipment = Shipment with { StatusHistories = historyResult.Data! };
                }
            }
            else
            {
                Snackbar.Add(result.Message ?? "Guncelleme basarisiz", Severity.Error);
            }
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private static Color GetStatusColor(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Created => Color.Default,
        ShipmentStatus.PickedUp => Color.Info,
        ShipmentStatus.InTransit => Color.Warning,
        ShipmentStatus.OutForDelivery => Color.Secondary,
        ShipmentStatus.Delivered => Color.Success,
        ShipmentStatus.ReturnedToSender => Color.Dark,
        ShipmentStatus.Failed => Color.Error,
        ShipmentStatus.Cancelled => Color.Default,
        _ => Color.Default
    };

    private static string GetStatusText(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Created => "Olusturuldu",
        ShipmentStatus.PickedUp => "Teslim Alindi",
        ShipmentStatus.InTransit => "Yolda",
        ShipmentStatus.OutForDelivery => "Dagitimda",
        ShipmentStatus.Delivered => "Teslim Edildi",
        ShipmentStatus.ReturnedToSender => "Gondericiye Iade",
        ShipmentStatus.Failed => "Basarisiz",
        ShipmentStatus.Cancelled => "Iptal",
        _ => "Bilinmiyor"
    };
}
