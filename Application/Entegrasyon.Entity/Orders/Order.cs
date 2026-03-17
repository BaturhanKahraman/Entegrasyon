using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Orders;

public sealed class Order : BaseEntity
{
    public Guid Id { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int TotalQuantity{ get; set; }//calculated
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal TotalPrice { get; set; }//calculated

    public IEnumerable<OrderItem> OrderItems { get; set; } = [];
    public Address BillingAddress { get; set; } = null!;
    public Address ShippingAddress { get; set; } = null!;

    // Marketplace sipariş bilgileri
    public int? MarketPlaceId { get; set; }
    public MarketPlace? MarketPlace { get; set; }

    public long? ShipmentPackageId { get; set; }

    [StringLength(100)]
    public string? OrderNumber { get; set; }

    [StringLength(50)]
    public string? MarketplaceOrderStatus { get; set; }

    [StringLength(100)]
    public string? CargoTrackingNumber { get; set; }

    [StringLength(500)]
    public string? CargoTrackingLink { get; set; }

    [StringLength(100)]
    public string? CargoProviderName { get; set; }

    public bool IsMicro { get; set; }
    public bool IsFastDelivery { get; set; }

    [Column(TypeName = "money")]
    public decimal? GrossAmount { get; set; }

    public DateTimeOffset? OrderDate { get; set; }
    public DateTimeOffset? EstimatedDeliveryEndDate { get; set; }

    // Müşteri bilgileri (Trendyol'dan gelen)
    [StringLength(100)]
    public string? CustomerFirstName { get; set; }

    [StringLength(100)]
    public string? CustomerLastName { get; set; }

    [StringLength(200)]
    public string? CustomerEmail { get; set; }
}
