using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class PayoutsVm
{
    public List<PayoutRequest> PendingPayouts { get; set; } = [];
}
