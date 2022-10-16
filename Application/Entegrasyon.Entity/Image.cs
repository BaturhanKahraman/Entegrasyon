using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity;

public sealed class Image : BaseEntity
{
    public int Id { get; set; }
    [StringLength(450)]
    [DataType("varchar")]
    public string Src { get; set; }
    [MaxLength(100)]
    public string AlternativeText { get; set; }
    [MaxLength(100)]
    public string Description { get; set; }

    public bool IsCoverImage { get; set; } = false;
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; }
}