using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Shared.Logic;
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
        var result = LogicRunner.Run(
            await CategoryExists(catId),
            await IsSuper(catId));
        if (result != null)
            return result;

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

    private async Task<IResult> IsSuper(int categoryId)
    {
        var isSuper = await _categoryManager.IsSuper(categoryId);
        if (isSuper)
            return new ErrorResult(Messages.CategoryIsSuper);
        return new SuccessResult();
    }
    private async Task<IResult> CategoryExists(int categoryId)
    {
        var exits =await _categoryManager.Exits(categoryId);
        if (!exits)
            return new ErrorResult(Messages.CategoryNotFound);
        return new SuccessResult();
    }
}
