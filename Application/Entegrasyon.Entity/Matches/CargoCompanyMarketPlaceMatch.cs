namespace Entegrasyon.Entity.Matches;

public sealed class CargoCompanyMarketPlaceMatch
{
    public int ApplicationCargoCompanyId { get; set; }
    public CargoCompany ApplicationCargoCompany { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;
    public int MarketPlaceCargoCompanyId { get; set; }
}