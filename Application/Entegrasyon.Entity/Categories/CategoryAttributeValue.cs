using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Entegrasyon.Entity.Categories;

public sealed class CategoryAttributeValue : BaseEntity
{
    public int Id { get; set; }
    [Required, StringLength(maximumLength: 35)]
    public string Name { get; set; }
    [NotMapped]
    public int TempMappingId { get; set; }
}