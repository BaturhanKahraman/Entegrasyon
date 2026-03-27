using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete;

public class CategoryMatchService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IFluentValidator fluentValidator) : ICategoryMatchService
{
    public async Task<CategoryMatchSummaryDto> GetCategoryMatchSummaryAsync()
    {
        using var dbContext = contextFactory.CreateDbContext();

        var totalCategories = await dbContext.Categories
            .Where(x => !x.IsDeleted)
            .CountAsync();

        var mappedCategories = await dbContext.CategoryMarketplaces
            .Where(x => x.MarketPlaceId == TrendyolMarketPlaceId && x.IsActive)
            .Select(x => x.CategoryId)
            .Distinct()
            .CountAsync();

        return new CategoryMatchSummaryDto
        {
            TotalCategories = totalCategories,
            MappedCategories = mappedCategories,
            UnmappedCategories = totalCategories - mappedCategories
        };
    }

    public async Task<List<CategoryMarketplaceMappingDto>> GetAllCategoryMappingsAsync(int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        return await dbContext.CategoryMarketplaces
            .Where(x => x.MarketPlaceId == marketPlaceId && x.IsActive)
            .Include(x => x.Category)
            .Select(x => new CategoryMarketplaceMappingDto
            {
                ApplicationCategoryId = x.CategoryId,
                ApplicationCategoryName = x.Category.Name,
                MarketPlaceId = x.MarketPlaceId,
                MarketPlaceCategoryId = x.MarketPlaceCategoryId,
                ExternalCategoryId = x.ExternalCategoryId,
                MarketPlaceCategoryName = x.MarketPlaceCategoryName
            })
            .ToListAsync();
    }

    public async Task<IResult> CreateCategoryMappingAsync(CreateCategoryMarketplaceMatchDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();

        await applicationLogManager.AddLog("Kategori mapping oluşturma isteği", LogType.Category, LogAction.Add, dto);

        // Validation
        if (dto.ApplicationCategoryId <= 0)
            return new ErrorResult("Geçerli bir kategori seçilmelidir.");

        if (dto.MarketPlaceCategoryId <= 0)
            return new ErrorResult("Marketplace kategori ID'si gereklidir.");

        // Business Rules
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == dto.ApplicationCategoryId && !x.IsDeleted);
        if (category is null)
        {
            var error = "Seçilen kategori bulunamadı veya silinmiş durumda.";
            await applicationLogManager.AddLog(error, LogType.Category, LogAction.Add, dto);
            return new ErrorResult(error);
        }

        var existingMapping = await dbContext.CategoryMarketplaces
            .FirstOrDefaultAsync(x =>
                x.CategoryId == dto.ApplicationCategoryId &&
                x.MarketPlaceId == dto.MarketPlaceId &&
                x.IsActive);

        if (existingMapping is not null)
        {
            var error = "Bu kategori için zaten bir mapping mevcuttur.";
            await applicationLogManager.AddLog(error, LogType.Category, LogAction.Add, dto);
            return new ErrorResult(error);
        }

        // Execution
        var mapping = new CategoryMarketplace
        {
            CategoryId = dto.ApplicationCategoryId,
            MarketPlaceId = dto.MarketPlaceId,
            MarketPlaceCategoryId = dto.MarketPlaceCategoryId,
            ExternalCategoryId = dto.ExternalCategoryId,
            MarketPlaceCategoryName = dto.MarketPlaceCategoryName,
            IsActive = true
        };

        dbContext.CategoryMarketplaces.Add(mapping);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("Kategori mapping başarıyla oluşturuldu", LogType.Category, LogAction.Add, dto);
        return new SuccessResult("Kategori mapping başarıyla oluşturuldu.");
    }

    public async Task<IDataResult<BulkCategoryMatchResultDto>> BulkCreateCategoryMappingsAsync(BulkCategoryMatchDto dto)
    {
        // 1. Validation
        var validationResult = await fluentValidator.Validate(dto);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorDataResult<BulkCategoryMatchResultDto>(new BulkCategoryMatchResultDto(), errorMessages);
        }

        using var dbContext = contextFactory.CreateDbContext();

        await applicationLogManager.AddLog(
            $"Toplu kategori eşleştirme isteği ({dto.Items.Count} öğe, MarketPlaceId: {dto.MarketPlaceId})",
            LogType.Category, LogAction.Add, dto);

        // 2. Business Rules: Load categories and existing mappings
        var requestedCategoryIds = dto.Items.Select(i => i.ApplicationCategoryId).Distinct().ToList();

        var existingCategories = await dbContext.Categories
            .Where(c => requestedCategoryIds.Contains(c.Id) && !c.IsDeleted)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var existingMappings = await dbContext.CategoryMarketplaces
            .Where(cm => cm.MarketPlaceId == dto.MarketPlaceId && cm.IsActive &&
                         requestedCategoryIds.Contains(cm.CategoryId))
            .Select(cm => cm.CategoryId)
            .ToListAsync();
        var existingMappingSet = existingMappings.ToHashSet();

        // 3. Execution
        var result = new BulkCategoryMatchResultDto { TotalRequested = dto.Items.Count };
        var newMappings = new List<CategoryMarketplace>();

        foreach (var item in dto.Items)
        {
            // Category not found or deleted
            if (!existingCategories.TryGetValue(item.ApplicationCategoryId, out var categoryName))
            {
                result.FailedCount++;
                result.Errors.Add(new BulkCategoryMatchErrorDto
                {
                    ApplicationCategoryId = item.ApplicationCategoryId,
                    CategoryName = item.MarketPlaceCategoryName ?? "Bilinmiyor",
                    ErrorMessage = "Kategori bulunamadı veya silinmiş durumda."
                });
                continue;
            }

            // Already mapped
            if (existingMappingSet.Contains(item.ApplicationCategoryId))
            {
                result.SkippedCount++;
                continue;
            }

            newMappings.Add(new CategoryMarketplace
            {
                CategoryId = item.ApplicationCategoryId,
                MarketPlaceId = dto.MarketPlaceId,
                MarketPlaceCategoryId = item.MarketPlaceCategoryId,
                ExternalCategoryId = item.ExternalCategoryId,
                MarketPlaceCategoryName = item.MarketPlaceCategoryName,
                IsActive = true
            });
            result.SuccessCount++;
        }

        if (newMappings.Count > 0)
        {
            dbContext.CategoryMarketplaces.AddRange(newMappings);
            await dbContext.SaveChangesAsync();
        }

        await applicationLogManager.AddLog(
            $"Toplu kategori eşleştirme tamamlandı: {result.SuccessCount} başarılı, {result.SkippedCount} atlandı, {result.FailedCount} hata",
            LogType.Category, LogAction.Add, result);

        return new SuccessDataResult<BulkCategoryMatchResultDto>(result, "Toplu eşleştirme tamamlandı.");
    }

    public async Task<IResult> RemoveCategoryMappingAsync(int categoryId, int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        await applicationLogManager.AddLog($"Kategori mapping silme isteği (CategoryId: {categoryId})", LogType.Category, LogAction.Delete);

        var mapping = await dbContext.CategoryMarketplaces
            .AsTracking()
            .FirstOrDefaultAsync(x =>
                x.CategoryId == categoryId &&
                x.MarketPlaceId == marketPlaceId &&
                x.IsActive);

        if (mapping is null)
        {
            var error = "Mapping bulunamadı.";
            await applicationLogManager.AddLog(error, LogType.Category, LogAction.Delete);
            return new ErrorResult(error);
        }

        dbContext.CategoryMarketplaces.Remove(mapping);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("Kategori mapping başarıyla silindi", LogType.Category, LogAction.Delete);
        return new SuccessResult("Kategori mapping başarıyla silindi.");
    }
}
