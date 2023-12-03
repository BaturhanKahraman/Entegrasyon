using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Entegrasyon.Entity.Categories;

public sealed class CategoryAttribute : BaseEntity
{
    public int Id { get; set; }
    public string CategoryAttributeKey { get; set; }
    public string CategoryAttributeHumanized { get; set; }

    public bool AllowCustom { get; set; }
    public int ImportId { get; set; }
    public List<CategoryAttributeValue> CategoryAttributeValues { get; set; } = new();
    public IEnumerable<CategoryAttributeCategory> Categories { get; set; }

    [NotMapped]
    public bool IsRequired { get; set; }
    [NotMapped]
    public bool IsVarianter { get; set; }
    [NotMapped]
    public bool IsSlicer { get; set; }
}