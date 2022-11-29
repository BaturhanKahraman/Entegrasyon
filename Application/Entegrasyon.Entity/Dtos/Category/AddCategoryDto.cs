using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public sealed record AddCategoryDto(string Name, ICollection<AddCategoryAttributeDto> CategoryAttributes,
    int? SuperCategoryId,bool IsFavorite);