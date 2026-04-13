using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.MVC.Features.POS.ViewModels;

public class POSTerminalVm
{
    public bool HasActiveSession { get; set; }
    public long SessionId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public string CashierName { get; set; } = "";
    public string? TerminalId { get; set; }
    public decimal OpeningCash { get; set; }
    public POSSummaryDto? Summary { get; set; }
}

public class POSCartVm
{
    public List<POSCartItemVm> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal VatTotal => Items.Sum(i => i.VatAmount);
    public decimal GrandTotal => Subtotal + VatTotal;
    public int TotalItems => Items.Sum(i => i.Quantity);
}

public class POSCartItemVm
{
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public string Title { get; set; } = "";
    public string Barcode { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public int Quantity { get; set; } = 1;

    public decimal LineTotal => UnitPrice * Quantity;
    public decimal VatAmount => LineTotal * VatRate / 100m;
}

public class POSSearchResultsVm
{
    public List<POSSearchItemVm> Products { get; set; } = [];
}

public class POSSearchItemVm
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = "";
    public string StockCode { get; set; } = "";
    public string BrandName { get; set; } = "";
    public int CurrentStock { get; set; }
}

public class POSPaymentDialogVm
{
    public long SessionId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public int ItemCount { get; set; }
    public List<PaymentMethodDefinition> PaymentMethods { get; set; } = [];
}

public class POSCloseSessionDialogVm
{
    public long SessionId { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public int TransactionCount { get; set; }
    public decimal ExpectedCash { get; set; }
}
