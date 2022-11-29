using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.EntityFrameworkCore;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeManager
{
    private readonly ICategoryAttributeDal _attributeDal;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly FluentValidator _fluentValidator;
    private readonly IMapper _mapper;
    public CategoryAttributeManager(ICategoryAttributeDal attributeDal, ApplicationLogManager applicationLogManager, FluentValidator fluentValidator, IMapper mapper)
    {
        _attributeDal = attributeDal;
        _applicationLogManager = applicationLogManager;
        _fluentValidator = fluentValidator;
        _mapper = mapper;
    }

    public async Task<bool> CheckIfExits(string name)
    {
        string nameNormalize = name.Trim().ToLower();
        return await _attributeDal.Table.AnyAsync(x => x.CategoryAttributeKey == nameNormalize);
    }

    public async Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributes()
    {
        return new SuccessDataResult<List<CategoryAttribute>>(await _attributeDal.GetAllAsync());
    }

    public async Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributesByCategory(int categoryId)
    {
        var result = await _attributeDal.GetTransformedEntitiesAsync(
            x => new CategoryAttribute
            {
                Id = x.Id, Required = x.Required, Slicer = x.Slicer, Varianter = x.Varianter,
                AllowCustom = x.AllowCustom, CreatedAt = x.CreatedAt,
                CategoryAttributeValues = x.CategoryAttributeValues, CategoryAttributeKey = x.CategoryAttributeKey
            },expression:x=>x.Categories.Any(c=>c.Id==categoryId));

        return new SuccessDataResult<List<CategoryAttribute>>(result);
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

}