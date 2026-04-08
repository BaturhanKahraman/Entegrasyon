using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Pricing;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Pricing;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class PricingRuleManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<PricingRuleManager> logger) : IPricingRuleManager
{
    public async Task<IDataResult<PricingRule>> CreateRuleAsync(CreatePricingRuleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new ErrorDataResult<PricingRule>(null!, "Kural adi bos olamaz.");
        if (dto.Value <= 0)
            return new ErrorDataResult<PricingRule>(null!, "Deger sifirdan buyuk olmalidir.");
        if (dto.RuleType == PricingRuleType.Percentage && dto.Value > 100)
            return new ErrorDataResult<PricingRule>(null!, "Yuzde degeri 100'den buyuk olamaz.");
        if (dto.Scope != PricingScope.AllProducts && !dto.ScopeEntityId.HasValue)
            return new ErrorDataResult<PricingRule>(null!, "Kapsam secildiginde hedef entity zorunludur.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var conflicting = await dbContext.PricingRules
            .AnyAsync(r => r.IsActive
                && r.MarketPlaceId == dto.MarketPlaceId
                && r.Scope == dto.Scope
                && r.ScopeEntityId == dto.ScopeEntityId);

        if (conflicting)
            return new ErrorDataResult<PricingRule>(null!, "Bu kapsam ve pazaryeri icin zaten aktif bir kural mevcut.");

        var rule = new PricingRule
        {
            Name = dto.Name,
            MarketPlaceId = dto.MarketPlaceId,
            RuleType = dto.RuleType,
            Value = dto.Value,
            Scope = dto.Scope,
            ScopeEntityId = dto.ScopeEntityId,
            Direction = dto.Direction,
            Priority = dto.Priority,
            IsActive = true
        };

        dbContext.PricingRules.Add(rule);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"Fiyatlandirma kurali olusturuldu: {rule.Name}", LogType.Product, LogAction.Add);
        logger.LogInformation("PricingRule created: {RuleId} {RuleName}", rule.Id, rule.Name);

        return new SuccessDataResult<PricingRule>(rule, "Kural basariyla olusturuldu.");
    }

    public async Task<IResult> UpdateRuleAsync(UpdatePricingRuleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new ErrorResult("Kural adi bos olamaz.");
        if (dto.Value <= 0)
            return new ErrorResult("Deger sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var rule = await dbContext.PricingRules.FindAsync(dto.Id);
        if (rule is null)
            return new ErrorResult("Kural bulunamadi.");

        rule.Name = dto.Name;
        rule.MarketPlaceId = dto.MarketPlaceId;
        rule.RuleType = dto.RuleType;
        rule.Value = dto.Value;
        rule.Scope = dto.Scope;
        rule.ScopeEntityId = dto.ScopeEntityId;
        rule.Direction = dto.Direction;
        rule.Priority = dto.Priority;

        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog($"Fiyatlandirma kurali guncellendi: {rule.Name}", LogType.Product, LogAction.Update);
        return new SuccessResult("Kural guncellendi.");
    }

    public async Task<IResult> DeleteRuleAsync(int ruleId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var rule = await dbContext.PricingRules.FindAsync(ruleId);
        if (rule is null)
            return new ErrorResult("Kural bulunamadi.");

        rule.IsDeleted = true;
        rule.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"Fiyatlandirma kurali silindi: {rule.Name}", LogType.Product, LogAction.Delete);
        return new SuccessResult("Kural silindi.");
    }

    public async Task<IResult> ToggleRuleAsync(int ruleId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var rule = await dbContext.PricingRules.FindAsync(ruleId);
        if (rule is null)
            return new ErrorResult("Kural bulunamadi.");

        rule.IsActive = !rule.IsActive;
        await dbContext.SaveChangesAsync();

        var msg = rule.IsActive ? "aktif edildi" : "pasife alindi";
        await applicationLogManager.AddLog($"Fiyatlandirma kurali {msg}: {rule.Name}", LogType.Product, LogAction.Update);
        return new SuccessResult($"Kural {msg}.");
    }

    public async Task<List<PricingRuleDto>> GetAllRulesAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var rules = await dbContext.PricingRules
            .AsNoTracking()
            .Include(r => r.MarketPlace)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var scopeNames = await ResolveScopeNamesAsync(dbContext, rules);

        return rules.Select(r => new PricingRuleDto(
            r.Id, r.Name,
            r.MarketPlace?.Name, r.MarketPlaceId,
            r.RuleType, r.Value,
            r.Scope, r.ScopeEntityId,
            scopeNames.GetValueOrDefault(r.Id),
            r.Direction, r.Priority, r.IsActive
        )).ToList();
    }

    public async Task<PricingRule?> GetApplicableRuleAsync(Guid variantId, int marketplaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant?.Product is null)
            return null;

        var rules = await dbContext.PricingRules
            .AsNoTracking()
            .Where(r => r.IsActive && (r.MarketPlaceId == marketplaceId || r.MarketPlaceId == null))
            .OrderBy(r => r.Priority)
            .ToListAsync();

        // En spesifik kural kazanır: Category > Brand > AllProducts (platform-specific > genel)
        return rules.FirstOrDefault(r => r.Scope == PricingScope.Category && r.ScopeEntityId == variant.Product.CategoryId)
            ?? rules.FirstOrDefault(r => r.Scope == PricingScope.Brand && r.ScopeEntityId == variant.Product.BrandId)
            ?? rules.FirstOrDefault(r => r.Scope == PricingScope.AllProducts && r.MarketPlaceId == marketplaceId)
            ?? rules.FirstOrDefault(r => r.Scope == PricingScope.AllProducts && r.MarketPlaceId == null);
    }

    public Task<decimal> CalculatePriceAsync(decimal salePrice, PricingRule rule)
    {
        var adjustment = rule.RuleType == PricingRuleType.Percentage
            ? salePrice * rule.Value / 100m
            : rule.Value;

        var result = rule.Direction == PricingDirection.Increase
            ? salePrice + adjustment
            : salePrice - adjustment;

        return Task.FromResult(Math.Max(result, 0.01m));
    }

    public async Task<List<PriceImpactDto>> PreviewRuleImpactAsync(int ruleId, int maxItems = 20)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var rule = await dbContext.PricingRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == ruleId);
        if (rule is null)
            return [];

        var query = dbContext.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Where(v => v.Product != null && !v.Product.IsDeleted);

        query = rule.Scope switch
        {
            PricingScope.Category => query.Where(v => v.Product!.CategoryId == rule.ScopeEntityId),
            PricingScope.Brand => query.Where(v => v.Product!.BrandId == rule.ScopeEntityId),
            _ => query
        };

        var variants = await query.Take(maxItems).ToListAsync();

        var impacts = new List<PriceImpactDto>();
        foreach (var v in variants)
        {
            var currentPrice = v.SalePrice;
            var newPrice = await CalculatePriceAsync(currentPrice, rule);
            impacts.Add(new PriceImpactDto(
                v.Id, v.Product!.Title, v.Product.StockCode,
                currentPrice, newPrice, newPrice - currentPrice));
        }

        return impacts;
    }

    private static async Task<Dictionary<int, string>> ResolveScopeNamesAsync(
        IntegrationDbContext dbContext, List<PricingRule> rules)
    {
        var result = new Dictionary<int, string>();

        var categoryIds = rules.Where(r => r.Scope == PricingScope.Category && r.ScopeEntityId.HasValue).Select(r => r.ScopeEntityId!.Value).Distinct().ToList();
        var brandIds = rules.Where(r => r.Scope == PricingScope.Brand && r.ScopeEntityId.HasValue).Select(r => r.ScopeEntityId!.Value).Distinct().ToList();

        var categoryNames = await dbContext.Categories.Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);
        var brandNames = await dbContext.Brands.Where(b => brandIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name);

        foreach (var rule in rules)
        {
            if (rule.ScopeEntityId is null) continue;
            var name = rule.Scope switch
            {
                PricingScope.Category => categoryNames.GetValueOrDefault(rule.ScopeEntityId.Value),
                PricingScope.Brand => brandNames.GetValueOrDefault(rule.ScopeEntityId.Value),
                _ => null
            };
            if (name is not null)
                result[rule.Id] = name;
        }

        return result;
    }
}
