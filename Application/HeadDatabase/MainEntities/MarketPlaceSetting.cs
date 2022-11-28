namespace MainDatabase.MainEntities;

public class MarketPlaceSetting
{
    public int Id { get; set; }
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; }

    public bool IsBasicAuth { get; set; }

    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
}