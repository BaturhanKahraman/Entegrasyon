namespace Entegrasyon.Entity.Dtos.Category;

public sealed record AddCategoryDto(string Name,IEnumerable<AddCategoryAttributeDto> CategoryAttributes,
    int? SuperCategoryId,bool IsFavorite, decimal? DefaultVatRate = null);