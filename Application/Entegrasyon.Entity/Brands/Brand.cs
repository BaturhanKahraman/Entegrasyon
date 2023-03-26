using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Brands;

public sealed class Brand : BaseEntity
{
    public int Id { get; set; }
    [StringLength(maximumLength: 55, MinimumLength = 1)]
    public string Name { get; set; }
    public IEnumerable<Product> Products { get; set; }
}