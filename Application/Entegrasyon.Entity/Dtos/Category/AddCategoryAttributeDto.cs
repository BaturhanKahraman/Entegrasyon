using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public sealed record AddCategoryAttributeDto(int Id,bool IsRequired,bool AllowCustom, bool IsVarianter,string CategoryAttributeKey,
    bool IsSlicer,string CategoryAttributeHumanized,List<CategoryAttributeValue> CategoryAttributeValues,int CategoryId);