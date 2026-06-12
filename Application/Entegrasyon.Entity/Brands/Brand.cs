using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Brands;

public sealed class Brand : BaseEntity
{
    public int Id { get; set; }
    [StringLength(maximumLength: 55, MinimumLength = 1)]
    public string Name { get; set; } = null!;
    [StringLength(maximumLength: 55)]
    public string? NormalizedName { get; set; }
    public string? SeoSlug { get; set; }

    /// <summary>Tedarikçi iletişim e-postası. Yüksek iadeli ürün bildirimi bu adrese gönderilir; boşsa in-app bildirime düşer.</summary>
    [StringLength(maximumLength: 255)]
    [EmailAddress]
    public string? SupplierEmail { get; set; }

    public IEnumerable<Product> Products { get; set; } = new List<Product>();
}