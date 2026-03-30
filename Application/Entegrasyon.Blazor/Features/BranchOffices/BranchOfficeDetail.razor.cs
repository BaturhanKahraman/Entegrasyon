using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Products;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BranchOffices;

public partial class BranchOfficeDetail : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] private IBranchOfficeManager BranchOfficeManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private BranchOffice? _branch;
    private bool _loading;
    private bool _saving;

    // General tab
    private string _editName = string.Empty;
    private bool _editIsDefault;

    // Stock tab
    private List<BranchStockItemDto> _stocks = [];
    private bool _showOnlyWithStock;
    private HashSet<BranchStockItemDto> _selectedStockItems = [];
    private IEnumerable<BranchStockItemDto> FilteredStocks =>
        _showOnlyWithStock ? _stocks.Where(s => s.CurrentStock > 0) : _stocks;

    // Movements tab
    private List<StockMovementViewDto> _stockMovements = [];
    private DateRange _movementDateRange = new(DateTime.Today.AddMonths(-1), DateTime.Today);

    // Marketplace tab
    private List<MarketPlaceWarehouse> _marketPlaceWarehouses = [];
    private List<MarketPlace> _availableMarketPlaces = [];
    private int _selectedMarketPlaceId;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        _branch = await BranchOfficeManager.GetBranchById(Id);
        if (_branch is not null)
        {
            _editName = _branch.Name ?? string.Empty;
            _editIsDefault = _branch.IsDefaultMarketPlaceStock;
        }

        await LoadStocksAsync();
        await LoadMovementsAsync();
        await LoadMarketPlacesAsync();
        _loading = false;
    }

    private async Task LoadStocksAsync()
    {
        var result = await BranchOfficeManager.GetBranchStocksAsync(Id);
        if (result.Success)
            _stocks = result.Data ?? [];
    }

    private async Task LoadMovementsAsync()
    {
        var from = _movementDateRange.Start.HasValue
            ? new DateTimeOffset(_movementDateRange.Start.Value, TimeSpan.Zero)
            : (DateTimeOffset?)null;
        var to = _movementDateRange.End.HasValue
            ? new DateTimeOffset(_movementDateRange.End.Value, TimeSpan.Zero)
            : (DateTimeOffset?)null;

        var result = await BranchOfficeManager.GetBranchStockMovementsAsync(Id, from, to);
        if (result.Success)
            _stockMovements = result.Data ?? [];
    }

    private async Task LoadMarketPlacesAsync()
    {
        var result = await BranchOfficeManager.GetBranchMarketPlacesAsync(Id);
        if (result.Success)
            _marketPlaceWarehouses = result.Data ?? [];
    }

    private async Task SaveGeneralAsync()
    {
        if (string.IsNullOrWhiteSpace(_editName)) return;

        _saving = true;
        var dto = new BranchOfficeEditDto(Id, _editName);
        var result = await BranchOfficeManager.Update(dto);
        _saving = false;

        if (result.Success)
        {
            Snackbar.Add("Depo güncellendi.", Severity.Success);
            _branch = result.Data;
        }
        else
        {
            Snackbar.Add(result.Message ?? "Güncelleme sırasında hata oluştu.", Severity.Error);
        }
    }

    private async Task OpenTransferDialog()
    {
        var parameters = new DialogParameters
        {
            { nameof(StockTransferDialog.SourceBranchId), Id },
            { nameof(StockTransferDialog.SelectedItems), _selectedStockItems.ToList() }
        };
        var dialog = await DialogService.ShowAsync<StockTransferDialog>("Stok Transfer", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            _selectedStockItems.Clear();
            await LoadStocksAsync();
            await LoadMovementsAsync();
        }
    }

    private async Task AddMarketPlaceAsync()
    {
        var result = await BranchOfficeManager.AddMarketPlaceWarehouseAsync(Id, _selectedMarketPlaceId);
        if (result.Success)
        {
            Snackbar.Add("Pazaryeri bağlantısı eklendi.", Severity.Success);
            _selectedMarketPlaceId = 0;
            await LoadMarketPlacesAsync();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Ekleme sırasında hata oluştu.", Severity.Error);
        }
    }

    private async Task RemoveMarketPlaceAsync(int warehouseId)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Bağlantıyı Kaldır",
            "Bu pazaryeri bağlantısını kaldırmak istediğinizden emin misiniz? Stok miktarları değişecektir.",
            yesText: "Kaldır",
            cancelText: "İptal");

        if (confirmed != true) return;

        var result = await BranchOfficeManager.RemoveMarketPlaceWarehouseAsync(warehouseId);
        if (result.Success)
        {
            Snackbar.Add("Pazaryeri bağlantısı kaldırıldı.", Severity.Success);
            await LoadMarketPlacesAsync();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Kaldırma sırasında hata oluştu.", Severity.Error);
        }
    }

    private static Color GetMovementColor(StockMovementType type) => type switch
    {
        StockMovementType.Sale or StockMovementType.MarketplaceSale => Color.Primary,
        StockMovementType.Transfer => Color.Warning,
        StockMovementType.Return => Color.Success,
        StockMovementType.ManualAdjustment => Color.Default,
        _ => Color.Default
    };

    private static string GetMovementLabel(StockMovementType type) => type switch
    {
        StockMovementType.InitialStock => "İlk Stok",
        StockMovementType.Sale => "Satış",
        StockMovementType.MarketplaceSale => "MP Satış",
        StockMovementType.Return => "İade",
        StockMovementType.ManualAdjustment => "Manuel",
        StockMovementType.Transfer => "Transfer",
        _ => type.ToString()
    };
}
