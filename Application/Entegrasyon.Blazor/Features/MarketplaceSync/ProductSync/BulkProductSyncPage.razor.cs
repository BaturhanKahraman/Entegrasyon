using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class BulkProductSyncPage : ComponentBase
{
    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ITrendyolProductService TrendyolService { get; set; } = null!;
    [Inject] private IDbContextFactory<IntegrationDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<MarketPlace> _marketplaces = [];
    private int _selectedMarketPlaceId = 1;
    private MarketplaceSyncState? _stateFilter;
    private string _searchKey = string.Empty;

    private List<ProductSyncListItemDto> _items = [];
    private HashSet<ProductSyncListItemDto> _selectedItems = [];

    private bool _loading = true;
    private bool _bulkBusy;
    private int _bulkProgress;

    protected override async Task OnInitializedAsync()
    {
        await LoadMarketplaces();
        await LoadGrid();
    }

    private async Task LoadMarketplaces()
    {
        using var dbContext = DbContextFactory.CreateDbContext();
        _marketplaces = await dbContext.MarketPlaces.AsNoTracking().OrderBy(m => m.Id).ToListAsync();
        if (_marketplaces.Count > 0 && !_marketplaces.Any(m => m.Id == _selectedMarketPlaceId))
            _selectedMarketPlaceId = _marketplaces.First().Id;
    }

    private async Task LoadGrid()
    {
        _loading = true;
        _selectedItems = [];
        var result = await SyncManager.GetProductSyncListAsync(
            _selectedMarketPlaceId, _stateFilter, _searchKey, 0, 200);
        if (result.Success && result.Data is not null)
            _items = result.Data.Items.ToList();
        _loading = false;
    }

    private async Task OnMarketplaceChanged(int marketPlaceId)
    {
        _selectedMarketPlaceId = marketPlaceId;
        await LoadGrid();
    }

    private async Task OnFilterChanged(MarketplaceSyncState? state)
    {
        _stateFilter = state;
        await LoadGrid();
    }

    private async Task OnSearchChanged()
    {
        await LoadGrid();
    }

    private void OnRowClick(DataGridRowClickEventArgs<ProductSyncListItemDto> args)
    {
        NavigationManager.NavigateTo($"/products/{args.Item.ProductId}/sync");
    }

    private async Task SingleSync(ProductSyncListItemDto item)
    {
        var result = await SyncManager.SyncProductAsync(item.ProductId, _selectedMarketPlaceId);
        Snackbar.Add(result.Message ?? "", result.Success ? Severity.Success : Severity.Error);
        if (result.Success) await LoadGrid();
    }

    private async Task BulkSync()
    {
        await RunBulkOperation(async item =>
            await SyncManager.SyncProductAsync(item.ProductId, _selectedMarketPlaceId));
    }

    private async Task BulkUpdate()
    {
        await RunBulkOperation(async item =>
            await SyncManager.SyncProductAsync(item.ProductId, _selectedMarketPlaceId));
    }

    private async Task BulkDelete()
    {
        if (_selectedMarketPlaceId != 1)
        {
            Snackbar.Add("Bu pazaryeri için silme henüz desteklenmiyor", Severity.Warning);
            return;
        }

        var confirm = await DialogService.ShowMessageBox(
            "Seçilenleri Sil",
            $"{_selectedItems.Count} ürünü Trendyol'dan silmek istediğinize emin misiniz?",
            yesText: "Sil",
            cancelText: "İptal");
        if (confirm != true) return;

        await RunBulkOperation(async item =>
            await TrendyolService.DeleteProductAsync(item.ProductId));
    }

    private async Task RunBulkOperation(Func<ProductSyncListItemDto, Task<Entegrasyon.Entity.Results.IResult>> operation)
    {
        _bulkBusy = true;
        _bulkProgress = 0;
        var selected = _selectedItems.ToList();
        int successCount = 0;
        int failCount = 0;

        try
        {
            foreach (var item in selected)
            {
                var result = await operation(item);
                if (result.Success) successCount++;
                else failCount++;
                _bulkProgress++;
                StateHasChanged();
            }

            Snackbar.Add($"{successCount} başarılı, {failCount} başarısız",
                failCount == 0 ? Severity.Success : Severity.Warning);

            await LoadGrid();
        }
        finally
        {
            _bulkBusy = false;
            _bulkProgress = 0;
        }
    }
}
