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
}