using Shared.Entity;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Categories;

public class Category : ApplicationEntity
{
    [ConcurrencyCheck]
    [MinLength(3), MaxLength(30)]
    public string Name { get; set; }
    public int? SuperCategoryId { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; }
    public ICollection<CategoryAttribute> CategoryAttributes { get; set; }
}