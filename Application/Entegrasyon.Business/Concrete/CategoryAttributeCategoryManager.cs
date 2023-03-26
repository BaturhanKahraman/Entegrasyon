using AutoMapper;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeCategoryManager
{
    private readonly ICategoryAttributeCategoryDal _attributeCategoryDal;
    private readonly CategoryAttributeManager _attributeManager;

    public CategoryAttributeCategoryManager(ICategoryAttributeCategoryDal attributeCategoryDal, CategoryAttributeManager attributeManager)
    {
        _attributeCategoryDal = attributeCategoryDal;
        _attributeManager = attributeManager;
    }

    public async Task<List<CategoryAttributeCategory>> UpdateRangeCategoryAttributeCategories(
        int categoryId, 
        List<EditCategoryAttributeDto> categoryAttributeManyToManyEntities)
    {
        var ids = categoryAttributeManyToManyEntities.Select(x => x.Id);
        var cacList =
            await _attributeCategoryDal
                .Table
                .Where(x => x.CategoryId == categoryId && ids.Contains(x.CategoryAttributeId))
                .Include(x => x.CategoryAttribute)
                .ThenInclude(x => x.CategoryAttributeValues)
                .ToListAsync();
        foreach (var cac in cacList)
        {
            var catAttrDto = categoryAttributeManyToManyEntities.Single(x => x.Id == cac.CategoryAttributeId);
            cac.CategoryAttribute.AllowCustom = catAttrDto.AllowCustom;
            cac.CategoryAttribute.CategoryAttributeHumanized = catAttrDto.CategoryAttributeHumanized;
            cac.CategoryAttribute.CategoryAttributeKey = catAttrDto.CategoryAttributeKey;
            foreach (var value in catAttrDto.CategoryAttributeValues)
            {
                var existingValue =
                    cac.CategoryAttribute.CategoryAttributeValues.SingleOrDefault(x => x.Id == value.Id);
                if (existingValue != null)
                {
                    existingValue.Name = value.Name;
                }
                else
                {
                    cac.CategoryAttribute.CategoryAttributeValues.Add(value);
                }
            }
            cac.IsRequired = catAttrDto.IsRequired;
            cac.IsSlicer = catAttrDto.IsSlicer;
            cac.IsVarianter = catAttrDto.IsVarianter;
        }

        return cacList;
        //await _attributeCategoryDal.UpdateRangeAsync(cacList);

    }
}


//await _attributeCategoryDal.GetTransformedEntitiesAsync(x=>
//    new CategoryAttributeCategory{
//        CategoryAttribute = new CategoryAttribute
//        {
//            IsDeleted = x.IsDeleted,
//            DeletedAt = x.DeletedAt,
//            CreatedAt = x.CategoryAttribute.CreatedAt,
//            UpdatedAt = x.CategoryAttribute.UpdatedAt,
//            Id = x.CategoryAttribute.Id,
//            CategoryAttributeKey = x.CategoryAttribute.CategoryAttributeKey,
//            CategoryAttributeHumanized = x.CategoryAttribute.CategoryAttributeHumanized,
//            AllowCustom = x.CategoryAttribute.AllowCustom,
//            ImportId = x.CategoryAttribute.ImportId,
//            CategoryAttributeValues =
//                new List<CategoryAttributeValue>(x.CategoryAttribute.CategoryAttributeValues),
//        },
//        CategoryAttributeId = x.CategoryAttributeId,
//        CategoryId = x.CategoryId,
//        CreatedAt = x.CreatedAt,
//        DeletedAt = x.DeletedAt,
//        IsDeleted = x.IsDeleted,
//        IsRequired = x.IsRequired,
//        IsVarianter = x.IsVarianter,
//        IsSlicer = x.IsSlicer,
//        UpdatedAt = x.UpdatedAt
//    },null,expression:x =>
//    x.CategoryId == categoryId && ids.Contains(x.CategoryAttributeId),true);