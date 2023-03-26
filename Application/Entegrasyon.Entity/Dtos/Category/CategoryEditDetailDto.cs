namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryEditDetailDto(
    int Id,
    string Name,
    IEnumerable<EditCategoryAttributeDto> CategoryAttributes,
    int? SuperCategoryId,
    bool IsFavorite
    );