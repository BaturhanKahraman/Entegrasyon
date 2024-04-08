namespace Entegrasyon.Entity.Dtos.Category;

public sealed record CategoryDetailDto(int Id,
    int TotalProductCount,
    string Name,
    int SubCategoryCount,
    bool IsFavorited,
    int AttributeCount,
    string SuperCategoryName
    );