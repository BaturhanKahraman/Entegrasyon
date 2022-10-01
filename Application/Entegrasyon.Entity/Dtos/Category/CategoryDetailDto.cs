using System.Collections.Specialized;

namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryDetailDto(int Id,
    int TotalProductCount,
    string Name,
    int SubCategoryCount);