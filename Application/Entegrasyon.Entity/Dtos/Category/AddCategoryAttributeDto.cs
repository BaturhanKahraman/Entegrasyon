using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public sealed record AddCategoryAttributeDto
{
    public int Id { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVarianter { get; set; }
    public string CategoryAttributeKey { get; set; } = null!;
    public bool IsSlicer { get; set; }
    public string CategoryAttributeHumanized { get; set; } = null!;
    public List<CategoryAttributeValue> CategoryAttributeValues { get; set; } = [];

    public AddCategoryAttributeDto()
    {
    }

    public AddCategoryAttributeDto(int id,
                                   bool isRequired,
                                   bool isVarianter,
                                   string categoryAttributeKey,
                                   bool isSlicer,
                                   string categoryAttributeHumanized,
                                   List<CategoryAttributeValue> categoryAttributeValues)
    {
        Id = id;
        IsRequired = isRequired;
        IsVarianter = isVarianter;
        CategoryAttributeKey = categoryAttributeKey;
        IsSlicer = isSlicer;
        CategoryAttributeHumanized = categoryAttributeHumanized;
        CategoryAttributeValues = categoryAttributeValues;
    }
}
