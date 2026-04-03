using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class CampaignsVm
{
    public List<StorefrontEmailCampaign> Campaigns { get; set; } = [];
}

public class CampaignCreateVm
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public CampaignTarget Target { get; set; }
}
