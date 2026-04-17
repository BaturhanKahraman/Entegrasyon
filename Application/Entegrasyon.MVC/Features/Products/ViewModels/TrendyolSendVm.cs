using Entegrasyon.Entity.Dtos.Product;

namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class TrendyolSendVm
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public bool IsApproved { get; set; }

    // Preflight checks
    public ProductSendPreflightDto? Preflight { get; set; }
    public bool PreflightPassed => Preflight?.AllPassed ?? false;

    // Preview (only loaded when preflight passes)
    public TrendyolSendPreviewDto? Preview { get; set; }

    // Override form
    public string? TitleOverride { get; set; }
    public string? DescriptionOverride { get; set; }

    // Variant price overrides
    public List<VariantOverrideVm> Variants { get; set; } = [];
}

public class VariantOverrideVm
{
    public Guid VariantId { get; set; }
    public string Barcode { get; set; } = "";
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? ListPriceOverride { get; set; }
    public decimal? SalePriceOverride { get; set; }
}
