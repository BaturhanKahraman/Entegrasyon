using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public sealed record AddCategoryDto(string Name,IEnumerable<CategoryAttribute> CategoryAttributes,int? SuperCategoryId);