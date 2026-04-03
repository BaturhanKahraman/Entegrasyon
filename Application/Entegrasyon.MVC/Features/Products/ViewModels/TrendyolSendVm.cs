using Entegrasyon.Entity.Dtos.Product;

namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class TrendyolSendVm
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";

    // Preflight checks
    public ProductSendPreflightDto? Preflight { get; set; }
    public bool PreflightPassed => Preflight?.AllPassed ?? false;

    // Preview (only loaded when preflight passes)
    public TrendyolSendPreviewDto? Preview { get; set; }

    // Override form
    public string? TitleOverride { get; set; }
    public string? DescriptionOverride { get; set; }
}
