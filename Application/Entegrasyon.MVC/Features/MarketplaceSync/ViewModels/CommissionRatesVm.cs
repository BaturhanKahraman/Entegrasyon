using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Marketplace;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

public class CommissionRatesVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public List<MarketPlace> MarketPlaces { get; set; } = [];
    public List<MarketplaceCommissionRateDto> Rates { get; set; } = [];
}
