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

    public CategoryAttributeManager(ICategoryAttributeDal attributeDal, ApplicationLogManager applicationLogManager)
    {
        _attributeDal = attributeDal;
        _applicationLogManager = applicationLogManager;
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

    public Task<IResult> AddCategoryAttribute(AddCategoryAttributeDto dto)
    {
        //validate
        throw new NotImplementedException();
    }
}