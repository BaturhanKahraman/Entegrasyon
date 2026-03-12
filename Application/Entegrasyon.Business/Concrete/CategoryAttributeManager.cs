using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeManager(IApplicationLogManager applicationLogManager, IFluentValidator fluentValidator, IMapper mapper, ICategoryService categoryService, IntegrationDbContext dbContext) : ICategoryAttributeManager
{
    public async Task<List<CategoryAttribute>> AddIfNotExits(IEnumerable<CategoryAttribute> attrs)
    {
        var list = attrs.ToList();
        var newAttrs = list.Where(x => x.Id <= 0).ToList();
        if (newAttrs.Any())
        {
            dbContext.CategoryAttributes.AddRange(newAttrs);
            await dbContext.SaveChangesAsync();
        }
        return list;
    }

    public async Task<bool> CheckIfCategoryHasCategoryAttribute(int categoryId) =>
        await dbContext.CategoryAttributes.AnyAsync(x => x.Categories.Any(c => c.CategoryId == categoryId));

    public async Task<bool> CheckIfExits(string name)
    {
        string nameNormalize = name.Trim().ToLower();
        return await dbContext.CategoryAttributes.AnyAsync(x => x.CategoryAttributeKey.Trim().ToLower() == nameNormalize);
    }

    public async Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributes() =>
        new SuccessDataResult<List<CategoryAttribute>>(
            await dbContext.CategoryAttributes
                .Include(x => x.CategoryAttributeValues)
                .ToListAsync());

    public async Task<IDataResult<List<CategoryAttributeDto>>> GetCategoryAttributesByCategory(int categoryId)
    {
        var result = await dbContext.CategoryAttributes
            .Where(x => x.Categories.Any(c => c.CategoryId == categoryId))
            .Select(x => new CategoryAttributeDto(
                x.Id,
                x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id).IsRequired,
                x.AllowCustom,
                x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id).IsVarianter,
                x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id).IsSlicer,
                x.CreatedAt,
                x.CategoryAttributeKey,
                x.CategoryAttributeHumanized,
                x.CategoryAttributeValues.ToList()))
            .ToListAsync();
        return new SuccessDataResult<List<CategoryAttributeDto>>(result);
    }

    public async Task<IDataResult<CategoryAttribute>> GetCategoryAttributeById(int id)
    {
        var attr = await dbContext.CategoryAttributes
            .Include(x => x.CategoryAttributeValues)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (attr is null)
            return new ErrorDataResult<CategoryAttribute>(null, "Özellik bulunamadı.");
        return new SuccessDataResult<CategoryAttribute>(attr);
    }

    public async Task<IResult> UpdateCategoryAttribute(EditCategoryAttributeDto dto)
    {
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
        await fluentValidator.ValidateAndThrowAsync(dto);
        var categoryAttr = mapper.Map<CategoryAttribute>(dto);
        dbContext.CategoryAttributes.Add(categoryAttr);
        await dbContext.SaveChangesAsync();
        return new SuccessResult();
    }

    public async Task RemoveAllAttributesByCategoryId(int categoryId)
    {
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
        dbContext.CategoryAttributes.RemoveRange(attrs);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<CategoryAttribute>> GetCategoryAttributesByIds(IEnumerable<int> ids) =>
        await dbContext.CategoryAttributes.Where(x => ids.Contains(x.Id)).ToListAsync();

    public async Task<Dictionary<int, AttributeMarketPlaceMatchDto>> GetAttributeMarketPlaceMatchesAsync()
    {
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
}
