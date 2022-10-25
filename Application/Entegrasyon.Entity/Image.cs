using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;
using Shared.Entity;
using Shared.FileStorage;

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
    public FileStorageType FileStorageType { get; set; }
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; }
}