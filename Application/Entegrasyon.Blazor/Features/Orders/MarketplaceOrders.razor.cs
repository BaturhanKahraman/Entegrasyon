using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Orders;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Orders;

public partial class MarketplaceOrders : ComponentBase
{
    [Inject] private IOrderManager OrderManager { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<Order> _orders = [];
    private List<Order> _filteredOrders = [];
    private bool _loading = true;
    private string _searchText = string.Empty;
    private int? _selectedMarketPlaceId = null;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        _loading = true;
        var result = await OrderManager.GetOrdersAsync(marketPlaceId: _selectedMarketPlaceId);
        if (result.Success)
        {
            _orders = result.Data;
            ApplyFilter();
        }
        else
        {
            Snackbar.Add(result.Message, Severity.Error);
        }
        _loading = false;
    }

    private async Task RefreshOrdersAsync()
    {
        await LoadOrdersAsync();
        Snackbar.Add("Siparişler yenilendi.", Severity.Success);
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            _filteredOrders = _orders;
            return;
        }

        var search = _searchText.ToLowerInvariant();
        _filteredOrders = _orders.Where(o =>
            (o.OrderNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (o.CustomerFirstName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (o.CustomerLastName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();
    }

    private async Task OnMarketPlaceFilterChanged(int? value)
    {
        _selectedMarketPlaceId = value;
        await LoadOrdersAsync();
    }

    private void OnRowClick(DataGridRowClickEventArgs<Order> args)
    {
        NavigationManager.NavigateTo($"/marketplace/orders/{args.Item.Id}");
    }

    private static Color GetStatusColor(string? status) => status switch
    {
        // Trendyol statuses
        "Created" or "New" => Color.Info,
        "Picking" => Color.Warning,
        "Invoiced" or "Approved" => Color.Primary,
        "Shipped" or "Delivered" or "Completed" => Color.Success,
        "Cancelled" or "UnSupplied" or "Rejected" => Color.Error,
        _ => Color.Info
    };
}
