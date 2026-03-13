using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public sealed record CategoryEditPageDto(
    Categories.Category Category,
    bool IsLeaf,
    List<CategoryAttributeDto> CategoryAttributes,
    List<CategoryAttribute> AllAttributes,
    List<Categories.Category> ValidParentCandidates,
    bool HasSoldProducts,
    bool HasProducts);
