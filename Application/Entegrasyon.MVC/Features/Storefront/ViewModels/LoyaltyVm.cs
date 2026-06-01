using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class LoyaltyVm
{
    public LoyaltyDashboardDto Dashboard { get; set; } = new(0, 0, 0, 0, []);
}

public class LoyaltyCustomerVm
{
    public int CustomerId { get; set; }
    public StorefrontLoyaltyPoints Balance { get; set; } = null!;
    public List<StorefrontLoyaltyTransaction> Transactions { get; set; } = [];
}
