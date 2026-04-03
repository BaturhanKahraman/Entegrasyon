namespace Entegrasyon.MVC.Features.Sales.ViewModels;

public class SaleEntryVm
{
    public List<SaleCartItemVm> Items { get; set; } = [];
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal VatTotal => Items.Sum(i => i.VatAmount);
    public decimal GrandTotal => Subtotal + VatTotal;
    public int TotalItems => Items.Sum(i => i.Quantity);
}

public class SaleCartItemVm
{
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public string Title { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string BrandName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public int Quantity { get; set; } = 1;

    public decimal LineTotal => UnitPrice * Quantity;
    public decimal VatAmount => LineTotal * VatRate / 100m;
}

public class SaleSearchResultsVm
{
    public List<SaleSearchItemVm> Products { get; set; } = [];
}

public class SaleSearchItemVm
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = "";
    public string StockCode { get; set; } = "";
    public string BrandName { get; set; } = "";
    public decimal SalePrice { get; set; }
    public int CurrentStock { get; set; }
}

public class SaleCustomerSearchVm
{
    public List<SaleCustomerItemVm> Customers { get; set; } = [];
}

public class SaleCustomerItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Type { get; set; } = "";
}
