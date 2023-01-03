using System.ComponentModel.DataAnnotations;
using Shared.Entity;

namespace Entegrasyon.Entity.Products;

public sealed class Brand : BaseEntity
{
    public int Id { get; set; }
    [Required, StringLength(maximumLength: 55,MinimumLength = 1)]
    public string Name { get; set; }
    public IEnumerable<Product> Products { get; set; }
}