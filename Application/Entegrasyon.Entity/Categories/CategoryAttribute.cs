
namespace Entegrasyon.Entity.Categories;

public sealed class CategoryAttribute : BaseEntity
{
    public int Id { get; set; }
    public string CategoryAttributeKey { get; set; } = null!;
    public string CategoryAttributeHumanized { get; set; } = null!;

    public bool AllowCustom { get; set; }
    public int ImportId { get; set; }
    public List<CategoryAttributeValue> CategoryAttributeValues { get; set; } = new();
    public IEnumerable<CategoryAttributeCategory> Categories { get; set; } = [];
}