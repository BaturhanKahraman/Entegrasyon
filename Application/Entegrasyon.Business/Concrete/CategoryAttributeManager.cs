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
        return await _attributeDal.Table.AnyAsync(x=>x.CategoryAttributeKey== nameNormalize);
    }

    public async Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributes()
    {
        return new SuccessDataResult<List<CategoryAttribute>>(await _attributeDal.GetAllAsync());
    }

    public async Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributesByCategory(int categoryId)
    {
        var result =await _attributeDal.GetAllAsync(x => x.Category.Any(z=>z.Id==categoryId));
        return new SuccessDataResult<List<CategoryAttribute>>(result);
    }
    public async Task<IResult> AddCategoryAttribute(AddCategoryAttributeDto dto)
    {
        //validate
        throw new NotImplementedException();
    }
}