using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public record UpdateCategoryDto(int Id,string Name,List<CategoryAttribute> CategoryAttributes,int? SuperCategoryId);