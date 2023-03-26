using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeManager
{
    private readonly ICategoryAttributeDal _attributeDal;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly FluentValidator _fluentValidator;
    private readonly IMapper _mapper;
    public CategoryAttributeManager(ICategoryAttributeDal attributeDal,ApplicationLogManager applicationLogManager,FluentValidator fluentValidator,IMapper mapper)
    {
        _attributeDal = attributeDal;
        _applicationLogManager = applicationLogManager;
        _fluentValidator = fluentValidator;
        _mapper = mapper;
    }

    public async Task<List<CategoryAttribute>> AddIfNotExits(IEnumerable<CategoryAttribute> attrs)
    {
        var list = attrs.ToList();
        await _attributeDal.AddRangeAsync(list.Where(x => x.Id <= 0));
        return list.ToList();
    }

    public async Task<bool> CheckIfCategoryHasCategoryAttribute(int categoryId)
    {
        return await _attributeDal.Exists(x => x.Categories.Any(c => c.CategoryId == categoryId));
    }

    public async Task<bool> CheckIfExits(string name)
    {
        string nameNormalize = name.Trim().ToLower();
        return await _attributeDal.Exists(x => x.CategoryAttributeKey.Trim().ToLower() == nameNormalize);
    }

    public async Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributes()
    {
        return new SuccessDataResult<List<CategoryAttribute>>(await _attributeDal.GetAllAsync());
    }



    public async Task<IDataResult<List<CategoryAttributeDto>>> GetCategoryAttributesByCategory(int categoryId)
    {
        var result = await _attributeDal.GetTransformedEntitiesAsync(
            x => new CategoryAttributeDto(
               x.Id,
               x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId==x.Id).IsRequired,
               x.AllowCustom,
               x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id).IsVarianter,
               x.Categories.FirstOrDefault(z => z.CategoryId == categoryId && z.CategoryAttributeId == x.Id).IsSlicer,
               x.CreatedAt,
               x.CategoryAttributeKey,
               x.CategoryAttributeHumanized,
               x.CategoryAttributeValues.ToList()
            ),expression: x => x.Categories.Any(c => c.CategoryId == categoryId));

        return new SuccessDataResult<List<CategoryAttributeDto>>(result);
    }

    public async Task<IResult> AddCategoryAttribute(AddCategoryAttributeDto dto)
    {
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var categoryAttr = _mapper.Map<CategoryAttribute>(dto);

        await _attributeDal.AddAsync(categoryAttr);
        return new SuccessResult();
    }
    public async Task<IResult> AddCategoryAttributeRange(IEnumerable<AddCategoryAttributeDto> dto)
    {
        //döngüye gerek var mı ?
        //içeriye gelen ve Id si olanları geldiği category'e many to many olarak eklemek gerekiyor mu ?
        await _fluentValidator.ValidateAndThrowAsync(dto);

        var categoryAttr = _mapper.Map<IEnumerable<CategoryAttribute>>(dto);
        await _attributeDal.AddRangeAsync(categoryAttr);
        return new SuccessResult();
    }

    public async Task RemoveAllAttributesByCategoryId(int categoryId)
    {
        if(await _attributeDal.Exists(x => x.Categories.Any(c => c.CategoryId == categoryId)))
        {
            var attrs = await _attributeDal.GetAllAsync(x => x.Categories.Any(c => c.CategoryId == categoryId),true);
            await _attributeDal.RemoveRangeAsync(attrs);
        }
    }

    public async Task RemoveAttributes(IEnumerable<CategoryAttribute> attrs)=>await _attributeDal.RemoveRangeAsync(attrs);

    public async Task<List<CategoryAttribute>> GetCategoryAttributesByIds(IEnumerable<int> ids)
    {
        return await _attributeDal.GetAllAsync(x => ids.Contains(x.Id),true);
    }
}