using Entegrasyon.Entity.Marketplace;

namespace Entegrasyon.Entity.Pricing;

public sealed class PricingRule : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? MarketPlaceId { get; set; }
    public MarketPlace? MarketPlace { get; set; }
    public PricingRuleType RuleType { get; set; }
    public decimal Value { get; set; }
    public PricingScope Scope { get; set; }
    public int? ScopeEntityId { get; set; }
    public PricingDirection Direction { get; set; } = PricingDirection.Increase;
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum PricingRuleType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum PricingScope
{
    AllProducts = 0,
    Category = 1,
    Brand = 2
}

public enum PricingDirection
{
    Increase = 0,
    Decrease = 1
}
