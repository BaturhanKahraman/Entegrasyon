using Entegrasyon.DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;

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

    
}