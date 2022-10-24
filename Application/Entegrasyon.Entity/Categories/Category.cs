using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Categories;

public sealed class Category : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public int? SuperCategoryId { get; set; }
    public Category SuperCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; }
    public ICollection<CategoryAttribute> CategoryAttributes { get; set; }
    public ICollection<MainProduct> Products { get; set; }
}