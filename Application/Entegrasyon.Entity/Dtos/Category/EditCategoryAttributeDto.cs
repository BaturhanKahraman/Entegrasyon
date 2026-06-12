using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public record EditCategoryAttributeDto(
    int Id,
    bool IsRequired,
    bool IsVarianter,
    string CategoryAttributeKey,
    bool IsSlicer,
    string CategoryAttributeHumanized,
    List<CategoryAttributeValue> CategoryAttributeValues,
    int CategoryId);