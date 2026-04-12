namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class StoreSettingsVm
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public bool IsPublished { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public List<VariantStorePriceVm> VariantPrices { get; set; } = [];
}

public class VariantStorePriceVm
{
    public Guid VariantId { get; set; }
    public string VariantName { get; set; } = "";
    public decimal SalePrice { get; set; }
    public decimal ECommercePrice { get; set; }
}
