using Entegrasyon.Business.Abstract;
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
    IApplicationLogManager applicationLogManager) : ICategoryMatchService
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
