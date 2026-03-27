using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class CategoryMatchValidationService(
    IDbContextFactory<IntegrationDbContext> contextFactory) : ICategoryMatchValidationService
{
    public async Task<IDataResult<CategoryMatchValidationResultDto>> ValidateCategoryMatchAsync(int categoryId, int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);

        if (category is null)
            return new ErrorDataResult<CategoryMatchValidationResultDto>(
                new CategoryMatchValidationResultDto(), "Kategori bulunamadı.");

        var result = new CategoryMatchValidationResultDto
        {
            CategoryId = categoryId,
            CategoryName = category.Name
        };

        // 1. Check category mapping
        var hasCategoryMapping = await dbContext.CategoryMarketplaces
            .AnyAsync(cm => cm.CategoryId == categoryId && cm.MarketPlaceId == marketPlaceId && cm.IsActive);

        result.HasCategoryMapping = hasCategoryMapping;
        if (!hasCategoryMapping)
        {
            result.Errors.Add("Kategori eşleştirmesi bulunamadı.");
            result.IsValid = false;
            return new SuccessDataResult<CategoryMatchValidationResultDto>(result);
        }

        // 2. Check required attributes
        var categoryAttributes = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == categoryId)
            .Include(cac => cac.CategoryAttribute)
            .ToListAsync();

        var mappedAttributeIds = await dbContext.CategoryAttributeMarketPlaceMatches
            .Where(m => m.MarketPlaceId == marketPlaceId)
            .Select(m => m.ApplicationCategoryAttributeId)
            .ToListAsync();
        var mappedAttributeIdSet = mappedAttributeIds.ToHashSet();

        var requiredAttrs = categoryAttributes.Where(a => a.IsRequired).ToList();
        var varianterAttrs = categoryAttributes.Where(a => a.IsVarianter).ToList();

        result.TotalRequiredAttributes = requiredAttrs.Count;
        result.MappedRequiredAttributes = requiredAttrs.Count(a => mappedAttributeIdSet.Contains(a.CategoryAttributeId));
        result.TotalVarianterAttributes = varianterAttrs.Count;
        result.MappedVarianterAttributes = varianterAttrs.Count(a => mappedAttributeIdSet.Contains(a.CategoryAttributeId));

        // Required attributes that are NOT mapped -> errors
        foreach (var attr in requiredAttrs.Where(a => !mappedAttributeIdSet.Contains(a.CategoryAttributeId)))
        {
            var name = attr.CategoryAttribute?.CategoryAttributeHumanized ?? $"Attribute #{attr.CategoryAttributeId}";
            result.Errors.Add($"Zorunlu özellik eşleştirilmemiş: {name}");
        }

        // Varianter attributes that are NOT mapped and NOT required -> warnings
        foreach (var attr in varianterAttrs.Where(a => !a.IsRequired && !mappedAttributeIdSet.Contains(a.CategoryAttributeId)))
        {
            var name = attr.CategoryAttribute?.CategoryAttributeHumanized ?? $"Attribute #{attr.CategoryAttributeId}";
            result.Warnings.Add($"Varianter özellik eşleştirilmemiş: {name}");
        }

        result.IsValid = result.Errors.Count == 0;
        return new SuccessDataResult<CategoryMatchValidationResultDto>(result);
    }

    public async Task<IDataResult<List<CategoryMatchValidationResultDto>>> ValidateAllMatchesAsync(int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var mappedCategoryIds = await dbContext.CategoryMarketplaces
            .Where(cm => cm.MarketPlaceId == marketPlaceId && cm.IsActive)
            .Select(cm => cm.CategoryId)
            .Distinct()
            .ToListAsync();

        var results = new List<CategoryMatchValidationResultDto>();

        foreach (var categoryId in mappedCategoryIds)
        {
            var validationResult = await ValidateCategoryMatchAsync(categoryId, marketPlaceId);
            if (validationResult.Success)
                results.Add(validationResult.Data);
        }

        return new SuccessDataResult<List<CategoryMatchValidationResultDto>>(results);
    }
}
