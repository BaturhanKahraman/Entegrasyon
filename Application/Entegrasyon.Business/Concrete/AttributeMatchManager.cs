using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class AttributeMatchManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IAttributeMatchManager
{
    public async Task<List<CategoryAttributeMarketPlaceMatch>> GetAttributeMatchesAsync(int marketPlaceId, List<int> attributeIds)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.CategoryAttributeMarketPlaceMatches
            .Where(x => x.MarketPlaceId == marketPlaceId && attributeIds.Contains(x.ApplicationCategoryAttributeId))
            .ToListAsync();
    }

    public async Task<List<CategoryAttributeValueMarketPlaceMatch>> GetValueMatchesAsync(int marketPlaceId, List<int> valueIds)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.CategoryAttributeValueMarketPlaceMatches
            .Where(x => x.MarketPlaceId == marketPlaceId && valueIds.Contains(x.ApplicationCategoryAttributeValueId))
            .ToListAsync();
    }

    public async Task<IResult> SaveAttributeMatchAsync(int applicationAttributeId, int marketPlaceId, int marketplaceAttributeId, string? externalId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.CategoryAttributeMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationCategoryAttributeId == applicationAttributeId &&
                x.MarketPlaceId == marketPlaceId);

        if (existing != null)
            return new ErrorResult($"Bu özellik bu marketplace için zaten eşleştirilmiş.");

        var reverseConflict = await dbContext.CategoryAttributeMarketPlaceMatches
            .AnyAsync(x =>
                x.MarketPlaceId == marketPlaceId &&
                x.MarketPlaceCategoryAttributeId == marketplaceAttributeId &&
                x.ApplicationCategoryAttributeId != applicationAttributeId);

        if (reverseConflict)
            return new ErrorResult("Bu pazaryeri özelliği başka bir özelliğinize zaten eşlenmiş.");

        dbContext.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
        {
            ApplicationCategoryAttributeId = applicationAttributeId,
            MarketPlaceId = marketPlaceId,
            MarketPlaceCategoryAttributeId = marketplaceAttributeId,
            MarketPlaceCategoryAttributeExternalId = externalId
        });

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Özellik eşleştirmesi başarıyla kaydedildi.");
    }

    public async Task<IResult> RemoveAttributeMatchAsync(int applicationAttributeId, int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var match = await dbContext.CategoryAttributeMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationCategoryAttributeId == applicationAttributeId &&
                x.MarketPlaceId == marketPlaceId);

        if (match == null)
            return new ErrorResult("Eşleştirme bulunamadı.");

        dbContext.CategoryAttributeMarketPlaceMatches.Remove(match);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Özellik eşleştirmesi başarıyla silindi.");
    }

    public async Task<IResult> SaveValueMatchAsync(int applicationValueId, int marketPlaceId, int marketplaceValueId, string? externalId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationCategoryAttributeValueId == applicationValueId &&
                x.MarketPlaceId == marketPlaceId);

        if (existing != null)
            return new ErrorResult($"Bu özellik değeri bu marketplace için zaten eşleştirilmiş.");

        dbContext.CategoryAttributeValueMarketPlaceMatches.Add(new CategoryAttributeValueMarketPlaceMatch
        {
            ApplicationCategoryAttributeValueId = applicationValueId,
            MarketPlaceId = marketPlaceId,
            MarketPlaceCategoryAttributeValueId = marketplaceValueId,
            MarketPlaceCategoryAttributeValueExternalId = externalId
        });

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Özellik değeri eşleştirmesi başarıyla kaydedildi.");
    }

    public async Task<IResult> RemoveValueMatchAsync(int applicationValueId, int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var match = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationCategoryAttributeValueId == applicationValueId &&
                x.MarketPlaceId == marketPlaceId);

        if (match == null)
            return new ErrorResult("Özellik değeri eşleştirmesi bulunamadı.");

        dbContext.CategoryAttributeValueMarketPlaceMatches.Remove(match);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Özellik değeri eşleştirmesi başarıyla silindi.");
    }
}
