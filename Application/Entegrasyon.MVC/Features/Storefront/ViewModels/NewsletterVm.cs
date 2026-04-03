using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class NewsletterVm
{
    public List<StorefrontNewsletter> Subscribers { get; set; } = [];
    public int ActiveCount => Subscribers.Count(s => s.IsActive);
    public int InactiveCount => Subscribers.Count(s => !s.IsActive);
}
