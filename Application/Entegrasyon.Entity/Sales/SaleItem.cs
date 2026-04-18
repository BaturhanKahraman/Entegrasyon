using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Sales;

public sealed class SaleItem:BaseEntity
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int? BranchOfficeId { get; set; }
    public BranchOffice? BranchOffice { get; set; }
    public double TaxPercentage { get; set; }
    public double DiscountPercent { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? UsedDiscountVoucherCode { get; set; }

    [StringLength(100)]
    public string Barcode { get; set; } = "";

    [StringLength(300)]
    public string ProductTitle { get; set; } = "";

    public int ReturnedQuantity { get; set; }

    /// <summary>POS'ta kalem başı sabit TL indirimi. DiscountPercent ile aynı anda > 0 olamaz.</summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>Opsiyonel seçilen standart indirim nedeni.</summary>
    public int? DiscountReasonId { get; set; }
    public DiscountReason? DiscountReason { get; set; }

    /// <summary>Opsiyonel serbest metin not.</summary>
    [StringLength(200)]
    public string? DiscountReasonNote { get; set; }
}