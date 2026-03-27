using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Orders;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Orders;

public partial class OrderDetail : ComponentBase
{
    [Parameter] public Guid OrderId { get; set; }

    [Inject] private IOrderManager OrderManager { get; set; } = null!;
    [Inject] private ITrendyolOrderService TrendyolOrderService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private Order? _order;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        var result = await OrderManager.GetOrderByIdAsync(OrderId);
        if (result.Success)
            _order = result.Data;
        else
            Snackbar.Add(result.Message ?? "", Severity.Error);
        _loading = false;
    }

    private void GoBack() => NavigationManager.NavigateTo("/marketplace/orders");

    private async Task MarkUnsuppliedAsync()
    {
        if (_order?.ShipmentPackageId is null) return;

        var lineIds = _order.OrderItems
            .Where(i => i.LineId.HasValue)
            .Select(i => i.LineId!.Value)
            .ToList();

        var result = await TrendyolOrderService.MarkUnsuppliedAsync(_order.ShipmentPackageId.Value, lineIds);
        if (result.Success)
        {
            _order.MarketplaceOrderStatus = "UnSupplied";
            Snackbar.Add("Sipariş tedarik edilemez olarak işaretlendi.", Severity.Success);
        }
        else
        {
            Snackbar.Add(result.Message ?? "", Severity.Error);
        }
    }

    private static Color GetStatusColor(string? status) => status switch
    {
        "Created" => Color.Info,
        "Picking" => Color.Warning,
        "Invoiced" => Color.Primary,
        "Shipped" => Color.Success,
        "Delivered" => Color.Success,
        "Cancelled" or "UnSupplied" => Color.Error,
        _ => Color.Default
    };
}
