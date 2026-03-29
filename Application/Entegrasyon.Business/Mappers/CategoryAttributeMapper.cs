using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class CategoryAttributeMapper
{
    [MapperIgnoreTarget(nameof(CategoryAttribute.Categories))]
    public partial CategoryAttribute MapToEntity(AddCategoryAttributeDto dto);

    /// <summary>
    /// Builds a CategoryAttributeCategory graph for an edit update.
    /// Mapperly cannot write into nested nav-property paths — this is a manual factory method.
    /// </summary>
    public static CategoryAttributeCategory ToJunctionUpdate(EditCategoryAttributeDto dto) => new()
    {
        CategoryAttributeId = dto.Id,
        CategoryAttribute = new CategoryAttribute
        {
            Id = dto.Id,
            CategoryAttributeKey = dto.CategoryAttributeKey,
            CategoryAttributeHumanized = dto.CategoryAttributeHumanized,
            CategoryAttributeValues = dto.CategoryAttributeValues,
            AllowCustom = dto.AllowCustom
        }
    };
}
