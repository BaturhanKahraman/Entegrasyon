namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryEditDetailDto(
    int Id,
    string Name,
    int? SuperCategoryId,
    bool IsFavorite
    );