using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Entegrasyon.Entity.Categories;

public class CategoryAttributeValue : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    [NotMapped]
    public int TempMappingId { get; set; }
}