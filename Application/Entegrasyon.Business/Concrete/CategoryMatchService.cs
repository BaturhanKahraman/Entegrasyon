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
    public async Task<CategoryMatchSummaryDto> GetCategoryMatchSummaryAsync(int? marketPlaceId = null)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var totalCategories = await dbContext.Categories
            .Where(x => !x.IsDeleted)
            .CountAsync();

        // When specific marketplace requested, filter by it
        IQueryable<CategoryMarketplace> mappingQuery = dbContext.CategoryMarketplaces
            .Where(x => x.IsActive);

        if (marketPlaceId.HasValue)
            mappingQuery = mappingQuery.Where(x => x.MarketPlaceId == marketPlaceId.Value);

        var mappedCategories = await mappingQuery
            .Select(x => x.CategoryId)
            .Distinct()
            .CountAsync();

        // Per-marketplace breakdown
        var perMarketplace = await dbContext.CategoryMarketplaces
            .Where(x => x.IsActive)
            .GroupBy(x => x.MarketPlaceId)
            .Select(g => new { MarketPlaceId = g.Key, MappedCount = g.Select(x => x.CategoryId).Distinct().Count() })
            .ToListAsync();

        var marketplaceNames = await dbContext.MarketPlaces
            .Where(mp => perMarketplace.Select(p => p.MarketPlaceId).Contains(mp.Id))
            .ToDictionaryAsync(mp => mp.Id, mp => mp.Name);

        return new CategoryMatchSummaryDto
        {
            TotalCategories = totalCategories,
            MappedCategories = mappedCategories,
            UnmappedCategories = totalCategories - mappedCategories,
            PerMarketplace = perMarketplace.Select(p => new MarketplaceMatchSummaryItemDto
            {
                MarketPlaceId = p.MarketPlaceId,
                MarketPlaceName = marketplaceNames.GetValueOrDefault(p.MarketPlaceId, $"Marketplace #{p.MarketPlaceId}"),
                MappedCount = p.MappedCount
            }).OrderByDescending(p => p.MappedCount).ToList()
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

    // ── Template Operations ───────────────────────────────────────

    public async Task<IDataResult<List<CategoryMatchTemplateDto>>> GetTemplatesAsync(int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var templates = await dbContext.CategoryMatchTemplates
            .Where(t => t.MarketPlaceId == marketPlaceId && !t.IsDeleted)
            .Select(t => new CategoryMatchTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                MarketPlaceId = t.MarketPlaceId,
                MappingCount = t.Items.Count,
                CreatedAt = t.CreatedAt
            })
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<CategoryMatchTemplateDto>>(templates);
    }

    public async Task<IDataResult<CategoryMatchTemplateDetailDto>> GetTemplateDetailAsync(int templateId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var template = await dbContext.CategoryMatchTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted);

        if (template is null)
            return new ErrorDataResult<CategoryMatchTemplateDetailDto>(
                new CategoryMatchTemplateDetailDto(), "Template bulunamadi.");

        var detail = new CategoryMatchTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            MarketPlaceId = template.MarketPlaceId,
            CreatedAt = template.CreatedAt,
            Mappings = template.Items.Select(i => new CategoryMatchTemplateMappingDto
            {
                ApplicationCategoryId = i.ApplicationCategoryId,
                ApplicationCategoryName = i.ApplicationCategoryName,
                MarketPlaceCategoryId = i.MarketPlaceCategoryId,
                ExternalCategoryId = i.ExternalCategoryId,
                MarketPlaceCategoryName = i.MarketPlaceCategoryName
            }).ToList()
        };

        return new SuccessDataResult<CategoryMatchTemplateDetailDto>(detail);
    }

    public async Task<IResult> SaveTemplateAsync(SaveCategoryMatchTemplateDto dto)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new ErrorResult("Template adi bos olamaz.");

        using var dbContext = contextFactory.CreateDbContext();

        // Business Rules: fetch current mappings
        var currentMappings = await dbContext.CategoryMarketplaces
            .Where(cm => cm.MarketPlaceId == dto.MarketPlaceId && cm.IsActive)
            .Include(cm => cm.Category)
            .ToListAsync();

        if (currentMappings.Count == 0)
            return new ErrorResult("Kaydedilecek eslestirme bulunamadi.");

        // Execution
        var template = new CategoryMatchTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            MarketPlaceId = dto.MarketPlaceId,
            Items = currentMappings.Select(cm => new CategoryMatchTemplateItem
            {
                ApplicationCategoryId = cm.CategoryId,
                ApplicationCategoryName = cm.Category?.Name ?? $"Kategori #{cm.CategoryId}",
                MarketPlaceCategoryId = cm.MarketPlaceCategoryId,
                ExternalCategoryId = cm.ExternalCategoryId,
                MarketPlaceCategoryName = cm.MarketPlaceCategoryName
            }).ToList()
        };

        dbContext.CategoryMatchTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Kategori eslestirme template'i kaydedildi: {dto.Name} ({currentMappings.Count} eslestirme)",
            LogType.Category, LogAction.Add, dto);

        return new SuccessResult($"Template basariyla kaydedildi ({currentMappings.Count} eslestirme).");
    }

    public async Task<IDataResult<BulkCategoryMatchResultDto>> ApplyTemplateAsync(int templateId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var template = await dbContext.CategoryMatchTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted);

        if (template is null)
            return new ErrorDataResult<BulkCategoryMatchResultDto>(
                new BulkCategoryMatchResultDto(), "Template bulunamadi.");

        // Convert template items to bulk match DTO and delegate to existing bulk logic
        var bulkDto = new BulkCategoryMatchDto
        {
            MarketPlaceId = template.MarketPlaceId,
            Items = template.Items.Select(i => new BulkCategoryMatchItemDto
            {
                ApplicationCategoryId = i.ApplicationCategoryId,
                MarketPlaceCategoryId = i.MarketPlaceCategoryId,
                ExternalCategoryId = i.ExternalCategoryId,
                MarketPlaceCategoryName = i.MarketPlaceCategoryName
            }).ToList()
        };

        return await BulkCreateCategoryMappingsAsync(bulkDto);
    }

    public async Task<IResult> DeleteTemplateAsync(int templateId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var template = await dbContext.CategoryMatchTemplates
            .AsTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && !t.IsDeleted);

        if (template is null)
            return new ErrorResult("Template bulunamadi.");

        template.IsDeleted = true;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Kategori eslestirme template'i silindi: {template.Name}",
            LogType.Category, LogAction.Delete);

        return new SuccessResult("Template basariyla silindi.");
    }
}
