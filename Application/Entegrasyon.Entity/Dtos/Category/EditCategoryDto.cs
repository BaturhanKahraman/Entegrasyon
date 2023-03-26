namespace Entegrasyon.Entity.Dtos.Category;

public record EditCategoryDto(
    int Id,
    string Name,
    List<EditCategoryAttributeDto> CategoryAttributes,
    int? SuperCategoryId,
    bool IsFavorite
    );