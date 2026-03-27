using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Matches;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeManager(IApplicationLogManager applicationLogManager, IFluentValidator fluentValidator, IMapper mapper, IDbContextFactory<IntegrationDbContext> contextFactory) : ICategoryAttributeManager
{
    public async Task<List<CategoryAttribute>> AddIfNotExits(IEnumerable<CategoryAttribute> attrs)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var list = attrs.ToList();
        var newAttrs = list.Where(x => x.Id <= 0).ToList();
        if (newAttrs.Any())
        {
            dbContext.CategoryAttributes.AddRange(newAttrs);
            await dbContext.SaveChangesAsync();
        }
        return list;
    }

    public async Task<bool> CheckIfCategoryHasCategoryAttribute(int categoryId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.CategoryAttributes.AnyAsync(x => x.Categories.Any(c => c.CategoryId == categoryId));
    }

    public async Task<bool> CheckIfExits(string name)
    {
        using var dbContext = contextFactory.CreateDbContext();
        string nameNormalize = name.Trim().ToLower();
        return await dbContext.CategoryAttributes.AnyAsync(x => x.CategoryAttributeKey!.Trim().ToLower() == nameNormalize);
    }

    public async Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributes()
    {
        using var dbContext = contextFactory.CreateDbContext();
        return new SuccessDataResult<List<CategoryAttribute>>(
            await dbContext.CategoryAttributes
                .Include(x => x.CategoryAttributeValues)
                .ToListAsync());
    }

    public async Task<IDataResult<Pageable<CategoryAttribute>>> GetCategoryAttributesPageable(SearchablePageDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var query = dbContext.CategoryAttributes
            .Include(x => x.CategoryAttributeValues)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(dto.FullTextSearchKey))
        {
            var search = dto.FullTextSearchKey.ToLower();
            query = query.Where(x =>
                EF.Functions.ILike(x.CategoryAttributeHumanized!, $"%{search}%") ||
                EF.Functions.ILike(x.CategoryAttributeKey!, $"%{search}%"));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.CategoryAttributeHumanized)
            .Skip(dto.PageIndex * dto.PageSize)
            .Take(dto.PageSize)
            .ToListAsync();

        return new SuccessDataResult<Pageable<CategoryAttribute>>(
            new Pageable<CategoryAttribute>(items, dto.PageIndex, dto.PageSize, total));
    }

    public async Task<IDataResult<List<CategoryAttributeDto>>> GetCategoryAttributesByCategory(int categoryId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var result = await dbContext.CategoryAttributes
            .AsSingleQuery()
            .Where(x => x.Categories.Any(c => c.CategoryId == categoryId))
            .Select(x => new CategoryAttributeDto(
                x.Id,
                x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id)!.IsRequired,
                x.AllowCustom,
                x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id)!.IsVarianter,
                x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id)!.IsSlicer,
                x.CreatedAt,
                x.CategoryAttributeKey!,
                x.CategoryAttributeHumanized!,
                x.CategoryAttributeValues.ToList()))
            .ToListAsync();
        return new SuccessDataResult<List<CategoryAttributeDto>>(result);
    }

    public async Task<IDataResult<CategoryAttribute>> GetCategoryAttributeById(int id)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var attr = await dbContext.CategoryAttributes
            .Include(x => x.CategoryAttributeValues)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (attr is null)
            return new ErrorDataResult<CategoryAttribute>(null!, "Özellik bulunamadı.");
        return new SuccessDataResult<CategoryAttribute>(attr);
    }

    public async Task<IResult> UpdateCategoryAttribute(EditCategoryAttributeDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();
        await fluentValidator.ValidateAndThrowAsync(dto);
        var attr = await dbContext.CategoryAttributes
            .AsTracking()
            .Include(x => x.CategoryAttributeValues)
            .FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (attr is null)
            return new ErrorResult("Özellik bulunamadı.");

        attr.CategoryAttributeKey = dto.CategoryAttributeKey;
        attr.CategoryAttributeHumanized = dto.CategoryAttributeHumanized;
        attr.AllowCustom = dto.AllowCustom;

        // Diff predefined values: remove deleted, add new
        var incomingIds = dto.CategoryAttributeValues?.Select(v => v.Id).Where(id => id > 0).ToHashSet() ?? [];
        var toRemove = attr.CategoryAttributeValues.Where(v => !incomingIds.Contains(v.Id)).ToList();
        if (toRemove.Count > 0)
            dbContext.CategoryAttributeValues.RemoveRange(toRemove);

        var existingIds = attr.CategoryAttributeValues.Select(v => v.Id).ToHashSet();
        var toAdd = dto.CategoryAttributeValues?.Where(v => v.Id <= 0 || !existingIds.Contains(v.Id)).ToList() ?? [];
        foreach (var val in toAdd)
        {
            val.CategoryAttributeId = attr.Id;
            attr.CategoryAttributeValues.Add(val);
        }

        // Update junction table flags if category context is provided
        if (dto.CategoryId > 0)
        {
            var junction = await dbContext.CategoryAttributeCategories
                .AsTracking()
                .FirstOrDefaultAsync(x => x.CategoryAttributeId == dto.Id && x.CategoryId == dto.CategoryId);
            if (junction is not null)
            {
                junction.IsRequired = dto.IsRequired;
                junction.IsVarianter = dto.IsVarianter;
                junction.IsSlicer = dto.IsSlicer;
            }
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Özellik başarıyla güncellendi.");
    }

    public async Task<IResult> DeleteCategoryAttribute(int id)
    {
        using var dbContext = contextFactory.CreateDbContext();
        bool inUse = await dbContext.AttributeKeyValues.AnyAsync(x => x.CategoryAttributeId == id);
        if (inUse)
        {
            // Soft-delete: FK referansları bozulmaz, mevcut ürünler etkilenmez
            var attr = await dbContext.CategoryAttributes.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (attr is null)
                return new ErrorResult("Özellik bulunamadı.");
            attr.IsDeleted = true;
            attr.DeletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();
            return new SuccessResult("Özellik ürünlerde kullanıldığından arşivlendi.");
        }

        var attrToDelete = await dbContext.CategoryAttributes.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (attrToDelete is null)
            return new ErrorResult("Özellik bulunamadı.");
        dbContext.CategoryAttributes.Remove(attrToDelete);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Özellik silindi.");
    }

    public async Task<IResult> AddCategoryAttribute(AddCategoryAttributeDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();
        await fluentValidator.ValidateAndThrowAsync(dto);
        var categoryAttr = mapper.Map<CategoryAttribute>(dto);
        dbContext.CategoryAttributes.Add(categoryAttr);
        await dbContext.SaveChangesAsync();
        return new SuccessResult();
    }

    public async Task RemoveAllAttributesByCategoryId(int categoryId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var attrs = await dbContext.CategoryAttributes
            .Where(x => x.Categories.Any(c => c.CategoryId == categoryId))
            .ToListAsync();
        if (attrs.Any())
        {
            dbContext.CategoryAttributes.RemoveRange(attrs);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveAttributes(IEnumerable<CategoryAttribute> attrs)
    {
        using var dbContext = contextFactory.CreateDbContext();
        dbContext.CategoryAttributes.RemoveRange(attrs);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<CategoryAttribute>> GetCategoryAttributesByIds(IEnumerable<int> ids)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.CategoryAttributes.Where(x => ids.Contains(x.Id)).ToListAsync();
    }

    public async Task<Dictionary<int, AttributeMarketPlaceMatchDto>> GetAttributeMarketPlaceMatchesAsync()
    {
        using var dbContext = contextFactory.CreateDbContext();
        return await dbContext.CategoryAttributeMarketPlaceMatches
            .AsNoTracking()
            .Include(m => m.MarketPlace)
            .GroupBy(m => m.ApplicationCategoryAttributeId)
            .Select(g => g.First())
            .ToDictionaryAsync(
                m => m.ApplicationCategoryAttributeId,
                m => new AttributeMarketPlaceMatchDto(
                    m.ApplicationCategoryAttributeId,
                    m.MarketPlace.Name,
                    m.MarketPlaceCategoryAttributeId));
    }

    public async Task<IResult> CreateAttributeMarketPlaceMatchAsync(CreateAttributeMarketPlaceMatchDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();

        await applicationLogManager.AddLog("Özellik marketplace mapping oluşturma isteği", LogType.Matching, LogAction.Add, dto);

        // Validation
        if (dto.ApplicationCategoryAttributeId <= 0)
            return new ErrorResult("Geçerli bir özellik seçilmelidir.");

        if (dto.MarketPlaceCategoryAttributeId <= 0)
            return new ErrorResult("Marketplace özellik ID'si gereklidir.");

        // Business Rules
        var attribute = await dbContext.CategoryAttributes.FirstOrDefaultAsync(x => x.Id == dto.ApplicationCategoryAttributeId);
        if (attribute is null)
        {
            var error = "Seçilen özellik bulunamadı.";
            await applicationLogManager.AddLog(error, LogType.Matching, LogAction.Add, dto);
            return new ErrorResult(error);
        }

        var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationCategoryAttributeId == dto.ApplicationCategoryAttributeId &&
                x.MarketPlaceId == dto.MarketPlaceId);

        if (existingMatch is not null)
        {
            var error = "Bu özellik için zaten bir eşleştirme mevcuttur.";
            await applicationLogManager.AddLog(error, LogType.Matching, LogAction.Add, dto);
            return new ErrorResult(error);
        }

        // Execution
        var match = new CategoryAttributeMarketPlaceMatch
        {
            ApplicationCategoryAttributeId = dto.ApplicationCategoryAttributeId,
            MarketPlaceId = dto.MarketPlaceId,
            MarketPlaceCategoryAttributeId = dto.MarketPlaceCategoryAttributeId
        };

        dbContext.CategoryAttributeMarketPlaceMatches.Add(match);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("Özellik marketplace mapping başarıyla oluşturuldu", LogType.Matching, LogAction.Add, dto);
        return new SuccessResult("Özellik eşleştirme başarıyla oluşturuldu.");
    }

    public async Task<IResult> RemoveAttributeMarketPlaceMatchAsync(int attributeId, int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        await applicationLogManager.AddLog($"Özellik marketplace mapping silme isteği (AttributeId: {attributeId})", LogType.Matching, LogAction.Delete);

        var match = await dbContext.CategoryAttributeMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationCategoryAttributeId == attributeId &&
                x.MarketPlaceId == marketPlaceId);

        if (match is null)
        {
            var error = "Eşleştirme bulunamadı.";
            await applicationLogManager.AddLog(error, LogType.Matching, LogAction.Delete);
            return new ErrorResult(error);
        }

        dbContext.CategoryAttributeMarketPlaceMatches.Remove(match);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("Özellik marketplace mapping başarıyla silindi", LogType.Matching, LogAction.Delete);
        return new SuccessResult("Özellik eşleştirme başarıyla silindi.");
    }
}
