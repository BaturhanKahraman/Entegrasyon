using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Entegrasyon.Entity.Categories;

public sealed class CategoryAttribute : BaseEntity
{
    public int Id { get; set; }
    public string CategoryAttributeKey { get; set; }
    public string CategoriyAttributeHumanized { get; set; }

    public bool AllowCustom { get; set; }
    public int ImportId { get; set; }
    public ICollection<CategoryAttributeValue> CategoryAttributeValues { get; set; }
    public IEnumerable<CategoryAttributeCategory> Categories { get; set; }

    [NotMapped]
    public int TempMappingId { get; set; }
    [NotMapped]
    public bool IsRequired { get; set; }
    [NotMapped]
    public bool IsVarianter { get; set; }
    [NotMapped]
    public bool IsSlicer { get; set; }
}