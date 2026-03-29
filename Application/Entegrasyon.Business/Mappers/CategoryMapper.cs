using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CategoryMapper
{
    // AddCategoryDto → Category: ignore nav-prop, conditional null for SuperCategoryId
    [MapperIgnoreTarget(nameof(Category.CategoryAttributes))]
    [MapProperty(nameof(AddCategoryDto.SuperCategoryId), nameof(Category.SuperCategoryId),
                 Use = nameof(ZeroToNull))]
    public partial Category MapToEntity(AddCategoryDto dto);

    public partial AddCategoryDto MapToDto(Category entity);

    // CategoryEditDetailDto ↔ Category
    public partial Category MapToEntity(CategoryEditDetailDto dto);
    public partial CategoryEditDetailDto MapToEditDetailDto(Category entity);

    // EditCategoryDto → Category: ignore nav-prop
    [MapperIgnoreTarget(nameof(Category.CategoryAttributes))]
    public partial Category MapToEntity(EditCategoryDto dto);
    public partial EditCategoryDto MapToEditDto(Category entity);

    // AddCategoryDtoStepOne ↔ Category
    public partial Category MapToEntity(AddCategoryDtoStepOne dto);
    public partial AddCategoryDtoStepOne MapToStepOneDto(Category entity);

    private static int? ZeroToNull(int? value) => value == 0 ? null : value;
}
