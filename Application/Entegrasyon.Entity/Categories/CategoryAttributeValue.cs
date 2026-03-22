using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Categories;

public class CategoryAttributeValue : BaseEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; } = null!;
}