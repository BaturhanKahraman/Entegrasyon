using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Shipping;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Shipping;

public partial class ShipmentTrackingPage : ComponentBase
{
    [Inject]
    private IShipmentTrackingManager ShipmentTrackingManager { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private List<ShipmentTrackingDto> _shipments = new();
    private CargoSummaryDto? _summary;
    private bool _isLoading;
    private bool _isLoadingSummary;

    // Filters
    private int? _selectedCargoCompanyId;
    private ShipmentStatus? _selectedStatus;
    private DateTime? _fromDate;
    private DateTime? _toDate;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        _isLoadingSummary = true;
        StateHasChanged();

        try
        {
            var summaryTask = ShipmentTrackingManager.GetCargoSummaryAsync();
            var shipmentsTask = ShipmentTrackingManager.GetAllShipmentsAsync(BuildFilter());

            await Task.WhenAll(summaryTask, shipmentsTask);

            var summaryResult = await summaryTask;
            if (summaryResult.Success)
                _summary = summaryResult.Data;

            var shipmentsResult = await shipmentsTask;
            if (shipmentsResult.Success)
                _shipments = shipmentsResult.Data ?? new List<ShipmentTrackingDto>();
        }
        finally
        {
            _isLoading = false;
            _isLoadingSummary = false;
        }
    }

    private ShipmentFilterDto BuildFilter() => new(
        Status: _selectedStatus,
        CargoCompanyId: _selectedCargoCompanyId,
        FromDate: _fromDate.HasValue ? new DateTimeOffset(_fromDate.Value, TimeSpan.Zero) : null,
        ToDate: _toDate.HasValue ? new DateTimeOffset(_toDate.Value.AddDays(1), TimeSpan.Zero) : null);

    private async Task OnCargoCompanyFilterChanged(int? value)
    {
        _selectedCargoCompanyId = value;
        await LoadShipmentsAsync();
    }

    private async Task OnStatusFilterChanged(ShipmentStatus? value)
    {
        _selectedStatus = value;
        await LoadShipmentsAsync();
    }

    private async Task OnFromDateChanged(DateTime? value)
    {
        _fromDate = value;
        await LoadShipmentsAsync();
    }

    private async Task OnToDateChanged(DateTime? value)
    {
        _toDate = value;
        await LoadShipmentsAsync();
    }

    private async Task LoadShipmentsAsync()
    {
        _isLoading = true;
        try
        {
            var result = await ShipmentTrackingManager.GetAllShipmentsAsync(BuildFilter());
            if (result.Success)
                _shipments = result.Data ?? new List<ShipmentTrackingDto>();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OnRowClicked(DataGridRowClickEventArgs<ShipmentTrackingDto> args)
    {
        var parameters = new DialogParameters<ShipmentDetailDialog>
        {
            { x => x.Shipment, args.Item }
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ShipmentDetailDialog>("Kargo Detayi", parameters, options);
        var result = await dialog.Result;

        // Refresh data after dialog closes
        await LoadDataAsync();
    }

    private async Task OnAddTrackingClicked()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<AddTrackingDialog>("Yeni Kargo Takibi", options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await LoadDataAsync();
        }
    }

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
