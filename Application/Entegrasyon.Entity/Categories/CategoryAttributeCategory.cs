
namespace Entegrasyon.Entity.Categories;

public class CategoryAttributeCategory:BaseEntity
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; } = null!;

    public bool IsRequired { get; set; }
    public bool IsSlicer { get; set; }
    public bool IsVarianter { get; set; }
    
}