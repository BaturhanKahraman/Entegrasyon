using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public record AddCategoryAttributeDto(bool IsRequired,bool AllowCustom, bool IsVarianter,
    bool IsSlicer,string CategoryAttributeHumanized,List<CategoryAttributeValue> CategoryAttributeValues,int CategoryId);