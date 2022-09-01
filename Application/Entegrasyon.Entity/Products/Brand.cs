using System.ComponentModel.DataAnnotations;
using Shared.Entity;

namespace Entegrasyon.Entity.Products;

public class Brand : ApplicationEntity
{
    [Required, StringLength(maximumLength: 55,MinimumLength = 1)]
    public string Name { get; set; }
    public List<MainProduct> Products { get; set; }
}