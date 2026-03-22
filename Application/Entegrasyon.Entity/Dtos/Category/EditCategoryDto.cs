namespace Entegrasyon.Entity.Dtos.Category;

public sealed record EditCategoryDto(
    int Id,
    string Name,
    int? SuperCategoryId,
    bool IsFavorite,
    bool IsImported,
    decimal? DefaultVatRate = null
    );