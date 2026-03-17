using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Orders;

public sealed class OrderItem : BaseEntity
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public ProductVariant? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Marketplace sipariş satır bilgileri
    public long? LineId { get; set; }

    [StringLength(100)]
    public string? Barcode { get; set; }

    [StringLength(100)]
    public string? MerchantSku { get; set; }

    [StringLength(100)]
    public string? ProductColor { get; set; }

    [StringLength(100)]
    public string? ProductSize { get; set; }

    [Column(TypeName = "money")]
    public decimal? Discount { get; set; }
}
