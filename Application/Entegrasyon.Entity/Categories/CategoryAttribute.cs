using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Entegrasyon.Entity.Categories;

public sealed class CategoryAttribute : BaseEntity
{
    public int Id { get; set; }
    [Required, StringLength(maximumLength: 55,MinimumLength = 3)]
    public string CategoryAttributeKey { get; set; }
    [StringLength(80)]
    public string CategoriyAttributeHumanized { get; set; }
    public bool Required { get; set; }
    public bool AllowCustom { get; set; }
    public bool Varianter { get; set; }
    public bool Slicer { get; set; }
    public ICollection<CategoryAttributeValue> CategoryAttributeValues { get; set; }
    public ICollection<Category> Category { get; set; }
    [NotMapped]
    public int TempMappingId { get; set; }
}