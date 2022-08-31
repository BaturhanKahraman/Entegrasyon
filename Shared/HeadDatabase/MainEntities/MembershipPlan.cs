namespace MainDatabase.MainEntities;

public class MembershipPlan
{
    public int Id { get; set; }
    public string Name { get; set; } //full, basic, free
    public List<MarketPlace> MarketPlaces { get; set; }
    public List<ApplicationProperty> ApplicationProperties { get; set; }
}