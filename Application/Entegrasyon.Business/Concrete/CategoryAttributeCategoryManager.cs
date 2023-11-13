using AutoMapper;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.EntityFrameworkCore;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeCategoryManager
{
    private readonly ICategoryAttributeCategoryDal _attributeCategoryDal;
    private readonly CategoryAttributeManager _attributeManager;
    private readonly CategoryManager _categoryManager;

    public CategoryAttributeCategoryManager(ICategoryAttributeCategoryDal attributeCategoryDal, CategoryAttributeManager attributeManager, CategoryManager categoryManager)
    {
        _attributeCategoryDal = attributeCategoryDal;
        _attributeManager = attributeManager;
        _categoryManager = categoryManager;
    }

    public async Task<IResult> AddCategoryAttributeForCategory(int catId, IEnumerable<AddCategoryAttributeDto> dto)
    {
        //validations & business logic must come here


       var catAttrCats= dto.Select(catAttr => new CategoryAttributeCategory()
        {
            CategoryAttribute = new CategoryAttribute
            {
                Id = catAttr.Id,
                CategoryAttributeKey = catAttr.CategoryAttributeKey,
                CategoryAttributeHumanized = catAttr.CategoryAttributeHumanized,
                CategoryAttributeValues = catAttr.CategoryAttributeValues
            },
            IsRequired = catAttr.IsRequired,
            IsSlicer = catAttr.IsSlicer,
            IsVarianter = catAttr.IsVarianter,
            CategoryId= catId
       }).ToList();
        await _attributeCategoryDal.AddRangeAsync(catAttrCats);
        return new SuccessResult();
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