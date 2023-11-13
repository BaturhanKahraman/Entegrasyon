using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public sealed record AddCategoryAttributeDto
{
    public int Id { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowCustom { get; set; }
    public bool IsVarianter { get; set; }
    public string CategoryAttributeKey { get; set; }
    public bool IsSlicer { get; set; }
    public string CategoryAttributeHumanized { get; set; }
    public List<CategoryAttributeValue> CategoryAttributeValues { get; set; }

    public AddCategoryAttributeDto()
    {
    }

    public AddCategoryAttributeDto(int id,
                                   bool isRequired,
                                   bool allowCustom,
                                   bool isVarianter,
                                   string categoryAttributeKey,
                                   bool isSlicer,
                                   string categoryAttributeHumanized,
                                   List<CategoryAttributeValue> categoryAttributeValues)
    {
        Id = id;
        IsRequired = isRequired;
        AllowCustom = allowCustom;
        IsVarianter = isVarianter;
        CategoryAttributeKey = categoryAttributeKey;
        IsSlicer = isSlicer;
        CategoryAttributeHumanized = categoryAttributeHumanized;
        CategoryAttributeValues = categoryAttributeValues;
    }
}
