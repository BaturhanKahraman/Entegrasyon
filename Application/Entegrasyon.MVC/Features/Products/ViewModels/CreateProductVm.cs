namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class CreateProductVm
{
    // Step 1: General
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string StockCode { get; set; } = "";
    public string? Season { get; set; }
    public string? Year { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string? BrandName { get; set; }
    public string? CategoryName { get; set; }

    // Step 2: Variants
    public List<CreateVariantVm> Variants { get; set; } = [new()];
}

public class CreateVariantVm
{
    public string Barcode { get; set; } = "";
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
    public decimal DimensionalWeight { get; set; }
    public int Stock { get; set; }
}
