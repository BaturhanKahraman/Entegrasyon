using Entegrasyon.Entity.Dtos.Pricing;
using Entegrasyon.Entity.Pricing;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPricingRuleManager
{
    Task<IDataResult<PricingRule>> CreateRuleAsync(CreatePricingRuleDto dto);
    Task<IResult> UpdateRuleAsync(UpdatePricingRuleDto dto);
    Task<IResult> DeleteRuleAsync(int ruleId);
    Task<IResult> ToggleRuleAsync(int ruleId);
    Task<List<PricingRuleDto>> GetAllRulesAsync();
    Task<PricingRule?> GetApplicableRuleAsync(Guid variantId, int marketplaceId);
    Task<decimal> CalculatePriceAsync(decimal salePrice, PricingRule rule);
    Task<List<PriceImpactDto>> PreviewRuleImpactAsync(int ruleId, int maxItems = 20);
}
