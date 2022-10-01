using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Categories;

public class Category : ApplicationEntity
{
    public string Name { get; set; }
    public int? SuperCategoryId { get; set; }
    public Category SuperCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; }
    public ICollection<CategoryAttribute> CategoryAttributes { get; set; }
    public ICollection<MainProduct> Products { get; set; }
}