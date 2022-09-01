using System.ComponentModel.DataAnnotations;
using Shared.Entity;

namespace Entegrasyon.Entity.Categories;

public class CategoryAttribute : ApplicationEntity
{
    [Required, StringLength(maximumLength: 55,MinimumLength = 3)]
    public string CategoryAttributeKey { get; set; }
    [StringLength(80)]
    public string CategoriyAttributeHumanized { get; set; }
    public bool Required { get; set; }
    public bool AllowCustom { get; set; }
    public bool Varianter { get; set; }
    public bool Slicer { get; set; }
    public List<CategoryAttributeValue> CategoryAttributeValues { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }


}