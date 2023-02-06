using Shared.Entity;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Categories;

public sealed class Category : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsImported { get; set; }
    public int? ImportId { get; set; }
    public int? SuperCategoryId { get; set; }
    public Category SuperCategory { get; set; }
    public IEnumerable<Category> SubCategories { get; set; }
    public List<CategoryAttributeCategory> CategoryAttributes { get; set; } = new ();
    public IEnumerable<Product> Products { get; set; }
}