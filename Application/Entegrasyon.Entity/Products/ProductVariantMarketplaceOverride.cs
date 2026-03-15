using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Products;

public sealed class ProductVariantMarketplaceOverride : BaseEntity
{
    public int Id { get; set; }
    public int ProductMarketplaceId { get; set; }
    public ProductMarketplace ProductMarketplace { get; set; } = null!;
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    [Column(TypeName = "money")]
    public decimal? ListPriceOverride { get; set; }

    [Column(TypeName = "money")]
    public decimal? SalePriceOverride { get; set; }
}
