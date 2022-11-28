using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public sealed record AddCategoryDto(string Name, ICollection<CategoryAttribute> CategoryAttributes,
    int? SuperCategoryId,bool IsFavorite);