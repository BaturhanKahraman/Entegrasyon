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

    protected override async Task OnInitializedAsync()
    {
        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        _loading = true;
        var result = await OrderManager.GetOrdersAsync(marketPlaceId: 1);
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

    private void OnRowClick(DataGridRowClickEventArgs<Order> args)
    {
        NavigationManager.NavigateTo($"/marketplace/orders/{args.Item.Id}");
    }

    private static Color GetStatusColor(string? status) => status switch
    {
        "Created" => Color.Info,
        "Picking" => Color.Warning,
        "Invoiced" => Color.Primary,
        "Shipped" => Color.Success,
        "Delivered" => Color.Success,
        "Cancelled" => Color.Error,
        "UnSupplied" => Color.Error,
        _ => Color.Default
    };
}
