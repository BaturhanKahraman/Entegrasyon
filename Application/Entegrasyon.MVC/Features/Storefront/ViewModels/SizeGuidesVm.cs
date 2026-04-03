using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class SizeGuidesVm
{
    public List<StorefrontSizeGuide> SizeGuides { get; set; } = [];
}

public class SizeGuideCreateVm
{
    public string Name { get; set; } = string.Empty;
    public string? CategoryIds { get; set; }
    public string? MeasurementInstructions { get; set; }
    public string SizeData { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
