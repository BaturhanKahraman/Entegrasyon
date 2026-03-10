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
        new SuccessDataResult<List<CategoryAttribute>>(await dbContext.CategoryAttributes.ToListAsync());

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
}
