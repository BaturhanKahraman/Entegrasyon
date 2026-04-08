using Entegrasyon.Entity.Pricing;

namespace Entegrasyon.Entity.Dtos.Pricing;

public record CreatePricingRuleDto(
    string Name,
    int? MarketPlaceId,
    PricingRuleType RuleType,
    decimal Value,
    PricingScope Scope,
    int? ScopeEntityId,
    PricingDirection Direction,
    int Priority);

public record UpdatePricingRuleDto(
    int Id,
    string Name,
    int? MarketPlaceId,
    PricingRuleType RuleType,
    decimal Value,
    PricingScope Scope,
    int? ScopeEntityId,
    PricingDirection Direction,
    int Priority);

public record PricingRuleDto(
    int Id,
    string Name,
    string? MarketPlaceName,
    int? MarketPlaceId,
    PricingRuleType RuleType,
    decimal Value,
    PricingScope Scope,
    int? ScopeEntityId,
    string? ScopeEntityName,
    PricingDirection Direction,
    int Priority,
    bool IsActive);

public record PriceImpactDto(
    Guid ProductVariantId,
    string ProductTitle,
    string? StockCode,
    decimal CurrentPrice,
    decimal NewPrice,
    decimal Difference);
